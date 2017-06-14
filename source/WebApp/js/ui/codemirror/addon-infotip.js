(function(mod) {
  if (typeof exports === "object" && typeof module === "object") // CommonJS
    mod(require("codemirror"));
  else if (typeof define === "function" && define.amd) // AMD
    define(["codemirror"], mod);
  else // Plain browser env
    mod(window.CodeMirror);
})(function(CodeMirror) {
  "use strict";

  var tooltip = (function() {
    var element;
    var ensureElement = function() {
      if (element)
        return;
      element = document.createElement("div");
      element.className = "CodeMirror-infotip cm-s-default"; // TODO: dynamic theme based on current cm
      element.setAttribute("hidden", "hidden");
      CodeMirror.on(element, "click", function() { tooltip.hide(); });
      document.getElementsByTagName("body")[0].appendChild(element);
    };

    return {
      show: function(content, x, y) {
        if (!this.active) {
          ensureElement();
          element.removeAttribute("hidden");
        }
        element.innerHTML = content;
        element.style.transform = "translate(" + x + "px, " + y + "px)";
        this.active = true;
        this.content = content;
      },

      hide: function() {
        if (!this.active || !element)
          return;
        element.setAttribute("hidden", "hidden");
        this.active = false;
      }
    };
  })();

  var activeTimeout;
  function mousemove(e) {
    /* eslint-disable no-invalid-this */
    if (activeTimeout) {
      clearTimeout(activeTimeout);
    }

    var wrapper = this;
    activeTimeout = setTimeout(function() {
      processMoveOrClick.call(wrapper, e);
      activeTimeout = null;
    }, 100);
  }

  function mouseout(e) {
    var cm = this.CodeMirror;
    if (e.target !== cm.getWrapperElement())
      return;
    tooltip.hide();
  }

  function click(e) {
    processMoveOrClick.call(this, e);
  }

  function processMoveOrClick(e) {
    /* eslint-disable no-invalid-this */
    var cm = this.CodeMirror;

    var coords = cm.coordsChar({ left: e.x, top: e.y });
    var getTipContent = cm.state.infotip.getTipContent || cm.getHelper(coords, "infotip");
    if (!getTipContent) {
      tooltip.hide();
      return;
    }

    var token = cm.getTokenAt(coords);
    var content = getTipContent(cm, token);
    if (content == null) {
      tooltip.hide();
      return;
    }

    if (tooltip.active && content === tooltip.content)
      return;
    const tokenStart = cm.cursorCoords(CodeMirror.Pos(coords.line, token.start));
    tooltip.show(content, tokenStart.left, tokenStart.bottom);
  }

  CodeMirror.defineOption("infotip", null, function(cm, options, old) {
    var wrapper = cm.getWrapperElement();
    var state = cm.state.infotip;
    if (old && old !== CodeMirror.Init && state) {
      CodeMirror.off(wrapper, "click",     click);
      CodeMirror.off(wrapper, "mousemove", mousemove);
      CodeMirror.off(wrapper, "mouseout",  mouseout);
      delete cm.state.infotip;
    }

    if (!options)
      return;

    state = {
      getTipContent: options.getTipContent
    };
    cm.state.infotip = state;
    CodeMirror.on(wrapper, "click",     click);
    CodeMirror.on(wrapper, "mousemove", mousemove);
    CodeMirror.on(wrapper, "mouseout",  mouseout);
  });
});