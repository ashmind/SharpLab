// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE
import CodeMirror from 'codemirror';

CodeMirror.defineMode('cil', () => {
    const grammar = {
        keyword: new RegExp('^(?:' + [
            /* spellchecker: disable */
            '\\.(?:class|custom|field|locals|method|maxstack)',
            'nested',
            'private', 'public', 'assembly', 'family', 'famorassem',
            'beforefieldinit', 'init',
            'ansi', 'auto', 'cil', 'managed',
            'extends', 'hidebysig', 'newslot', 'virtual', 'abstract', 'sealed',
            'instance', 'static',
            'specialname', 'rtspecialname'
            /* spellchecker: enable */
        ].join('|') + ')(?:$|\\s)'),
        builtin: new RegExp('^(?:' + [
            /* spellchecker: disable */
            'add', 'add.ovf', 'add.ovf.un',
            'and',
            'arglist',
            'beq', 'beq.s',
            'b[gl][et]', 'b[gl][et].s', 'b[gl][et].un', 'b[gl][et].un.s',
            'bne.un', 'bne.un.s',
            'box',
            'br(?:true|false|inst|null|zero)?(.s)?',
            'break',
            'call', 'calli', 'callvirt',
            'castclass',
            'ceq',
            'c[gl]t', 'c[gl]t.un',
            'ckfinite',
            'constrained.',
            'conv.i[1248]?', 'conv.ovf.i[1248]?', 'conv.ovf.i[1248]?.un',
            'conv.ovf.u[1248]?', 'conv.ovf.u[1248]?.un', 'conv.r.un', 'conv.r[48]', 'conv.u[1248]?',
            'cpblk',
            'cpobj',
            'div', 'div.un',
            'dup',
            'end(?:fault|filter|finally)',
            'initblk',
            'initobj',
            'isinst',
            'jmp',
            'ldarga?', 'ldarg.[0123s]', 'ldarga.s',
            'ldc.i[48]', 'ldc.i4.[0-8s]', 'ldc.i4.[mM]1', 'ldc.r[48]',
            'ldelema?', 'ldelem.i[1248]?', 'ldelem.r[48]', 'ldelem.ref', 'ldelem.u[1248]',
            'lds?flda?',
            'ldftn',
            'ldind.i[1248]?', 'ldind.r[48]', 'ldind.ref', 'ldind.u[1248]',
            'ldlen',
            'ldloca?', 'ldloc.[0123s]', 'ldloca.s',
            'ld(?:null|obj|str|token|virtftn)',
            'leave', 'leave.s',
            'localloc',
            'mkrefany',
            'mul', 'mul.ovf', 'mul.ovf.un',
            'neg',
            'newarr',
            'newobj',
            'nop',
            'not',
            'or',
            'pop',
            'readonly.',
            'refany(?:type|val)',
            'rem', 'rem.un',
            'ret',
            'rethrow',
            'sh[lr]', 'shr.un',
            'sizeof',
            'starg', 'starg.s',
            'stelem', 'stelem.i[1248]?', 'stelem.r[48]', 'stelem.ref',
            'sts?fld',
            'stind.i[1248]?', 'stind.r[48]', 'stind.ref',
            'stloc', 'stloc.[0123s]',
            'stobj',
            'sub', 'sub.ovf', 'sub.ovf.un',
            'switch',
            'tail.',
            'throw',
            'unaligned.',
            'unbox', 'unbox.any',
            'volatile.',
            'xor'
            /* spellchecker: enable */
        ].join('|').replace('.', '\\.') + ')(?:$|\\s)')
    };

    return {
        startState() {
            return {};
        },

        token(stream) {
            if (stream.eatSpace()) {
                return null;
            }

            if (stream.match('//')) {
                stream.skipToEnd();
                return 'comment';
            }

            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            if (stream.match(/\d+/)) {
                return 'number';
            }

            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            if (stream.match(/"[^"]+"/)) {
                return 'string';
            }

            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            if (stream.match(/\w+:/)) {
                return 'tag';
            }

            for (const key in grammar) {
                // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
                if (stream.match(grammar[key as keyof typeof grammar], false)) {
                    stream.match(/\S+/);
                    return key;
                }
            }

            stream.match(/\S+/);
            return null;
        }
    };
});

CodeMirror.defineMIME('text/x-cil', 'cil');