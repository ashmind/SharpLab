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

  class FlowLayer {
    constructor(cm) {
      const sizer = cm.getWrapperElement()
        .querySelector(".CodeMirror-sizer");
      const svg = createSVG("svg", {
        "class": "CodeMirror-flow-layer",
        width:  sizer.offsetWidth,
        height: sizer.offsetHeight
      });
      sizer.appendChild(svg);

      cm.on("update", debounce(() => this.resize(), 100));
      this.cm = cm;
      this.sizer = sizer;
      this.root = svg;
      this.rendered = {};
    }

    resize() {
      this.root.setAttribute("width", this.sizer.offsetWidth);
      this.root.setAttribute("height", this.sizer.offsetHeight);
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

      const offsetY = 4;
      const fromY = from.y + offsetY;
      const toY = to.y - offsetY;

      const g = this.renderSVG("g", {
        "class": "CodeMirror-flow-jump" + (up ? " CodeMirror-flow-jump-up" : "") + (options.throw ? " CodeMirror-flow-jump-throw" : ""),
        "data-debug": key
      });
      this.renderSVG(g, "path", {
        "class": "CodeMirror-flow-jump-line",
        d: `M ${from.x} ${fromY} H ${left} V ${toY} H ${to.x}`
      });
      this.renderSVG(g, "circle", {
        "class": "CodeMirror-flow-jump-start",
        cx: from.x, cy: fromY, r: 1.5
      });
      this.renderSVG(g, "path", {
        "class": "CodeMirror-flow-jump-arrow",
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
      return this.cm.cursorCoords(leftmost, "local").left - 15;
    }

    getJumpCoordinates(line) {
      const start = this.getLineStart(line);
      const coords = this.cm.cursorCoords({ ch: start, line }, "local");
      return {
        x: Math.round(100 * (coords.left - 5)) / 100,
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

  CodeMirror.defineExtension("addFlowJump", function(fromLine, toLine, options) {
    /* eslint-disable no-invalid-this */
    let flow = this.state.flow;
    if (!flow) {
      flow = new FlowLayer(this);
      this.state.flow = flow;
    }
    flow.renderJump(fromLine, toLine, options);
  });

  CodeMirror.defineExtension("clearFlowPoints", function() {
    /* eslint-disable no-invalid-this */
    const flow = this.state.flow;
    if (!flow)
      return;
    flow.clear();
  });
});
