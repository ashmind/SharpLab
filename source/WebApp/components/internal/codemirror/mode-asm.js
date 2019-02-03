// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE

(function(mod) {
  if (typeof exports === "object" && typeof module === "object") // CommonJS
    mod(require("codemirror"));
  else if (typeof define === "function" && define.amd) // AMD
    define(["codemirror"], mod);
  else // Plain browser env
    mod(window.CodeMirror);
})(function(CodeMirror) {
"use strict";

  CodeMirror.defineMode("asm", function() {
    "use strict";

    var grammar = {
      builtin: new RegExp("^(?:" + [
        "aa[adms]", "adc", "add", "and", "arpl",
        "bound", "bsf", "bsr", "bswap", "bt", "bt[crs]",
        "call", "cbw", "cdq", "cl[cdi]", "clts", "cmc", conditional("cmov"), "cmp", "cmps[bdw]", "cmpxchg", "cmpxchg8b", "cpuid", "cwd", "cwde",
        "daa", "das", "dec", "div",
        "enter", "esc",
        "hlt",
        "idiv", "imul", "in", "inc", "ins", "insd", "int", "into", "invd", "invlpg", "iret", "iret[df]",
        conditional("j"), "jecxz", "jcxz", "jmp",
        "lahf", "lar", "lds", "lea", "leave", "les", "l[gil]dt", "lfs", "lgs", "lmsw", "loadall", "lock", "lods[bdw]", "loop", "loop[dw]?", "loopn?[ez][dw]?", "lsl", "lss", "ltr",
        "mov", "movs[bdw]", "mov[sz]x", "mul",
        "neg", "nop", "not",
        "or", "out", "outsd?",
        "pop", "pop[af]d?", "push", "push[af]d?",
        "rcl", "rcr", "rdmsr", "rdpmc", "rdtsc", "rep", "repn?[ez]", "ret", "retn", "retf", "rol", "ror", "rsm",
        "sahf", "sal", "sar", "sbb", "scas[bdw]", conditional("set"), "s[gil]dt", "shld?", "shrd?", "smsw", "st[cdi]", "stos[bdw]", "str", "sub", "syscall", "sysenter", "sysexit", "sysret",
        "test",
        "ud2",
        "verr", "verw",
        "wait", "wbinvd", "wrmsr",
        "xadd", "xchg", "xlat", "xor"
      ].join("|") + ")(?:$|\\s)")
    };

    function conditional(mnemonic) {
      return mnemonic + "(?:n?[abgl]e?|n?[ceosz]|np|p[eo]?)";
    }

    return {
      startState: function() {
        return {};
      },

      token: function(stream) {
        if (stream.eatSpace()) {
          return null;
        }

        if (stream.eat(";")) {
          stream.skipToEnd();
          return "comment";
        }

        if (stream.match(/\w+:/)) {
          return "tag";
        }

        for (var key in grammar) {
          if (stream.match(grammar[key])) {
            return key;
          }
        }

        stream.match(/\S+/);
        return null;
      }
    };
  });

  CodeMirror.defineMIME("text/x-asm", "asm");
});