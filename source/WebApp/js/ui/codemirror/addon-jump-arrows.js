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

  function createSVG(tagName, attributes) {
    const element = document.createElementNS("http://www.w3.org/2000/svg", tagName);
    for (const key in attributes) {
      element.setAttribute(key, attributes[key]);
    }
    return element;
  }

  class ArrowLayer {
    constructor(cm) {
      const wrapper = cm.getWrapperElement();
      const scroll = wrapper.querySelector(".CodeMirror-scroll");
      const sizer = wrapper.querySelector(".CodeMirror-sizer");
      const svg = createSVG("svg", {
        "class": "CodeMirror-jump-arrow-layer",
        width:  sizer.offsetWidth,
        height: sizer.offsetHeight
      });
      scroll.appendChild(svg);

      cm.on("update", debounce(() => this.resize(), 100));
      this.cm = cm;
      this.sizer = sizer;
      this.sizerLeftMargin = parseInt(sizer.style.marginLeft);
      this.root = svg;
      this.rendered = {};
    }

    resize() {
      this.root.setAttribute("width", this.sizer.offsetWidth);
      this.root.setAttribute("height", this.sizer.offsetHeight);
      this.sizerLeftMargin = parseInt(this.sizer.style.marginLeft);
    }

    renderJump(fromLine, toLine, options) {
      const key = fromLine + "->" + toLine;
      if (this.rendered[key])
        return;

      const from = this.getJumpCoordinates(fromLine);
      const to = this.getJumpCoordinates(toLine);

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

      const g = this.renderSVG("g", { class: groupClassName });
      this.renderSVG(g, "path", {
        class: "CodeMirror-jump-arrow-line",
        d: `M ${from.x} ${fromY} H ${left} V ${toY} H ${to.x}`
      });
      this.renderSVG(g, "circle", {
        class: "CodeMirror-jump-arrow-start",
        cx: from.x, cy: fromY, r: 1.5
      });
      this.renderSVG(g, "path", {
        class: "CodeMirror-jump-arrow-end",
        d: `M ${to.x} ${toY} l -2 -1 v 2 z`
      });
      this.rendered[key] = true;
    }

    renderSVG(parent, tagName, attributes) {
      if (typeof parent === "string") {
        attributes = tagName;
        tagName = parent;
        parent = this.root;
      }

      const child = createSVG(tagName, attributes);
      parent.appendChild(child);
      return child;
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

    getJumpCoordinates(line) {
      const start = this.getLineStart(line);
      const coords = this.cm.cursorCoords({ ch: start, line }, "local");
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

    clear() {
      while (this.root.firstChild) {
        this.root.removeChild(this.root.firstChild);
      }
      this.rendered = [];
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
    layer.renderJump(fromLine, toLine, options);
  });

  CodeMirror.defineExtension("clearJumpArrows", function() {
    /* eslint-disable no-invalid-this */
    const layer = this.state[STATE_KEY];
    if (!layer)
      return;
    layer.clear();
  });
});
