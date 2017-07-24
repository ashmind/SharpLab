//! Copyright (c) Andrey Shchekin (MIT, see LICENSE.txt)
(function(mod) {
  "use strict";
  if (typeof exports === "object" && typeof module === "object") // CommonJS
    mod(require("codemirror"), require("codemirror/addon/lint/lint"));
  else if (typeof define === "function" && define.amd) // AMD
    define(["codemirror", "codemirror/addon/lint/lint"], mod);
  else // Plain browser env
    mod(window.CodeMirror);
})(function(CodeMirror) {
  "use strict";
  function debounce(func, interval) {
    let timer = null;
    return () => {
      if (timer)
        return;
      timer = setTimeout(() => { func(); timer = null; }, interval);
    };
  }

  function createSVGElement(tagName) {
    return document.createElementNS("http://www.w3.org/2000/svg", tagName);
  }

  function setAttributes(element, attributes) {
    for (const key in attributes) {
      element.setAttribute(key, attributes[key]);
    }
  }

  class ArrowLayer {
    constructor(cm) {
      const wrapper = cm.getWrapperElement();
      const scroll = wrapper.querySelector(".CodeMirror-scroll");
      const sizer = wrapper.querySelector(".CodeMirror-sizer");
      const svg = createSVGElement("svg");
      setAttributes(svg, { class: "CodeMirror-jump-arrow-layer" });
      this.root = svg;
      this.sizer = sizer;
      this.resize();
      scroll.appendChild(svg);

      this.cm = cm;
      this.jumps = {};
      cm.on("update", debounce(() => this.resize(), 100));
      cm.on("changes", () => this.repositionJumps());
    }

    resize() {
      setAttributes(this.root, {
        width:  this.sizer.offsetWidth,
        height: this.sizer.offsetHeight
      });
      this.sizerLeftMargin = parseInt(this.sizer.style.marginLeft);
    }

    repositionJumps() {
      for (const [key, jump] of Object.entries(this.jumps)) {
        const newFrom = jump.bookmarks.from.find();
        const newTo = jump.bookmarks.to.find();
        if (!newFrom || !newTo) {
          this.clearJump(jump);
          delete this.jumps[jump.key];
          continue;
        }

        const unchanged = newFrom.line === jump.from.line
                       && newFrom.ch === jump.from.ch
                       && newTo.ch === jump.to.ch
                       && newTo.line === jump.to.line;
        if (unchanged)
          continue;
        delete this.jumps[key];
        const rendered = this.renderJump(newFrom.line, newTo.line, {
          options: jump.options,
          svgToReuse: jump.svg,
          bookmarksToReuse: jump.bookmarks
        });
        if (!rendered)
          this.clearJump(jump);
      }
    }

    renderJump(fromLine, toLine, {options, svgToReuse, bookmarksToReuse}) {
      const key = fromLine + "->" + toLine;
      if (this.jumps[key])
        return false;

      const positions = {
        from: { line: fromLine, ch: this.getLineStart(fromLine) },
        to: { line: toLine, ch: this.getLineStart(toLine) }
      };

      const from = this.getJumpCoordinates(positions.from);
      const to = this.getJumpCoordinates(positions.to);

      const leftmost = this.getLeftmostBound(fromLine, toLine);
      let left = leftmost;
      let up = false;
      if (to.y < from.y) {
        // up
        left -= 8;
        up = true;
      }

      if (left < 1)
        left = 1;

      const offsetY = 4;
      const fromY = from.y + offsetY;
      const toY = to.y - offsetY;

      const groupClassName = "CodeMirror-jump-arrow"
        + (up ? " CodeMirror-jump-arrow-up" : "")
        + (options.throw ? " CodeMirror-jump-arrow-throw" : "");

      const {g, path, start, end} = svgToReuse || {
        g:     createSVGElement("g"),
        path:  createSVGElement("path"),
        start: createSVGElement("circle"),
        end:   createSVGElement("path")
      };
      setAttributes(g, { class: groupClassName });
      setAttributes(path, {
        class: "CodeMirror-jump-arrow-line",
        d: `M ${from.x} ${fromY} H ${left} V ${toY} H ${to.x}`
      });
      setAttributes(start, {
        class: "CodeMirror-jump-arrow-start",
        cx: from.x, cy: fromY, r: 1.5
      });
      setAttributes(end, {
        class: "CodeMirror-jump-arrow-end",
        d: `M ${to.x} ${toY} l -2 -1 v 2 z`
      });
      if (!svgToReuse) {
        g.appendChild(path);
        g.appendChild(start);
        g.appendChild(end);
        this.root.appendChild(g);
      }
      this.jumps[key] = {
        key,
        from: positions.from,
        to: positions.to,
        bookmarks: {
          from: bookmarksToReuse ? bookmarksToReuse.from : this.cm.setBookmark(positions.from),
          to: bookmarksToReuse ? bookmarksToReuse.to : this.cm.setBookmark(positions.to)
        },
        options,
        svg: { g, path, start, end }
      };
      return true;
    }

    clearJump(jump) {
      this.clearBookmarks(jump);
      this.root.removeChild(jump.svg.g);
    }

    clearBookmarks(jump) {
      jump.bookmarks.from.clear();
      jump.bookmarks.to.clear();
    }

    getLeftmostBound(fromLine, toLine) {
      const firstLine = Math.min(fromLine, toLine);
      const lastLine = Math.max(fromLine, toLine);

      let leftmost = { ch: 9999 };
      for (let line = firstLine; line <= lastLine; line++) {
        const lineStart = this.getLineStart(line);
        if (lineStart < leftmost.ch)
          leftmost = { line, ch: lineStart };
      }
      const coords =  this.cm.cursorCoords(leftmost, "local");
      return (coords.left + this.sizerLeftMargin) - 15;
    }

    getJumpCoordinates(position) {
      const coords = this.cm.cursorCoords(position, "local");
      const left = coords.left + this.sizerLeftMargin;
      return {
        x: Math.round(100 * (left - 5)) / 100,
        y: Math.round(100 * (coords.top + ((coords.bottom - coords.top) / 2))) / 100
      };
    }

    getLineStart(line) {
      const match = /[^\s]/.exec(this.cm.getLine(line));
      return match ? match.index : 9999;
    }

    replaceJumps(data) {
      const existing = Object.values(this.jumps);
      this.jumps = {};
      let nextIndexToReuse = 0;
      for (const jumpData of data) {
        const jumpToReuse = existing[nextIndexToReuse];
        if (jumpToReuse)
          this.clearBookmarks(jumpToReuse);
        const rendered = this.renderJump(jumpData.fromLine, jumpData.toLine, {
          options: jumpData.options || {},
          svgToReuse: jumpToReuse ? jumpToReuse.svg : null
        });
        if (rendered)
          nextIndexToReuse += 1;
      }

      for (let i = nextIndexToReuse; i < existing.length; i++) {
        this.clearJump(existing[i]);
      }
    }
  }

  const STATE_KEY = "jumpArrowLayer";
  CodeMirror.defineExtension("addJumpArrow", function(fromLine, toLine, options) {
    /* eslint-disable no-invalid-this */
    let layer = this.state[STATE_KEY];
    if (!layer) {
      layer = new ArrowLayer(this);
      this.state[STATE_KEY] = layer;
    }
    layer.renderJump(fromLine, toLine, { options: options || {} });
  });

  CodeMirror.defineExtension("setJumpArrows", function(arrows) {
    /* eslint-disable no-invalid-this */
    let layer = this.state[STATE_KEY];
    if (!layer) {
      layer = new ArrowLayer(this);
      this.state[STATE_KEY] = layer;
    }
    layer.replaceJumps(arrows);
  });

  CodeMirror.defineExtension("clearJumpArrows", function() {
    /* eslint-disable no-invalid-this */
    const layer = this.state[STATE_KEY];
    if (!layer)
      return;
    layer.replaceJumps([]);
  });
});
