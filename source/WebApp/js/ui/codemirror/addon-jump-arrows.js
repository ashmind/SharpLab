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
        const newFrom = jump.from.bookmark.find();
        const newTo = jump.to.bookmark.find();
        if (!newFrom || !newTo) {
          this.removeJump(jump, key);
          continue;
        }

        const unchanged = newFrom.line === jump.from.saved.line
                       && newFrom.ch === jump.from.saved.ch
                       && newTo.ch === jump.to.saved.ch
                       && newTo.line === jump.to.saved.line;
        if (unchanged)
          continue;
        delete this.jumps[key];
        this.renderJump(newFrom.line, newTo.line, {
          throw: jump.throw,
          existing: jump
        });
      }
    }

    renderJump(fromLine, toLine, options) {
      const key = fromLine + "->" + toLine;
      if (this.jumps[key])
        return;

      const fromPos = { line: fromLine, ch: this.getLineStart(fromLine) };
      const toPos = { line: toLine, ch: this.getLineStart(toLine) };

      const from = this.getJumpCoordinates(fromPos);
      const to = this.getJumpCoordinates(toPos);

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

      const existing = options.existing;
      const {g, path, start, end} = existing ? existing.svg : {
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
      if (!existing) {
        g.appendChild(path);
        g.appendChild(start);
        g.appendChild(end);
        this.root.appendChild(g);
      }
      this.jumps[key] = {
        from: {
          saved: fromPos,
          bookmark: this.cm.setBookmark(fromPos)
        },
        to: {
          saved: toPos,
          bookmark: this.cm.setBookmark(toPos)
        },
        throw: options.throw,
        svg: { g, path, start, end }
      };
    }

    removeJump(jump, key) {
      jump.from.bookmark.clear();
      jump.to.bookmark.clear();
      this.root.removeChild(jump.svg.g);
      if (key)
        delete this.jumps[key];
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
      for (let i = 0; i < data.length; i++) {
        const jumpData = data[i];
        this.renderJump(jumpData.fromLine, jumpData.toLine, {
          throw: (jumpData.options || {}).throw,
          existing: existing[i]
        });
      }

      for (let i = data.length; i < existing.length; i++) {
        this.removeJump(existing[i]);
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
    layer.renderJump(fromLine, toLine, options || {});
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
