// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE
import CodeMirror from 'codemirror';

CodeMirror.defineMode('asm', () => {
    'use strict';

    const grammar = {
        builtin: new RegExp('^(?:' + [
            /* spellchecker: disable */
            'aa[adms]', 'adc', 'add', 'and', 'arpl',
            'bound', 'bsf', 'bsr', 'bswap', 'bt', 'bt[crs]',
            'call', 'cbw', 'cdq', 'cl[cdi]', 'clts', 'cmc', conditional('cmov'), 'cmp', 'cmps[bdw]', 'cmpxchg', 'cmpxchg8b', 'cpuid', 'cwd', 'cwde',
            'daa', 'das', 'dec', 'div',
            'enter', 'esc',
            'hlt',
            'idiv', 'imul', 'in', 'inc', 'ins', 'insd', 'int', 'int[13o]', 'invd', 'invlpg', 'iret', 'iret[df]',
            conditional('j'), 'jecxz', 'jcxz', 'jmp',
            'lahf', 'lar', 'lds', 'lea', 'leave', 'les', 'l[gil]dt', 'lfs', 'lgs', 'lmsw', 'loadall', 'lock', 'lods[bdw]', 'loop', 'loop[dw]?', 'loopn?[ez][dw]?', 'lsl', 'lss', 'ltr',
            'mov', 'movs[bdw]', 'mov[sz]x', 'movsxd', 'mul',
            'neg', 'nop', 'not',
            'or', 'out', 'outsd?',
            'pop', 'pop[af]d?', 'push', 'push[af]d?',
            'rcl', 'rcr', 'rdmsr', 'rdpmc', 'rdtsc', 'rep', 'repn?[ez]', 'ret', 'retn', 'retf', 'rol', 'ror', 'rsm',
            'sahf', 'sal', 'sar', 'sbb', 'scas[bdw]', conditional('set'), 's[gil]dt', 'shld?', 'shrd?', 'smsw', 'st[cdi]', 'stos[bdw]', 'str', 'sub', 'syscall', 'sysenter', 'sysexit', 'sysret',
            'test',
            'ud2',
            'verr', 'verw',
            'vmovdqu', 'vmovdqu8', 'vmovdqu16', 'vmovdqu32', 'vxorps',
            'wait', 'wbinvd', 'wrmsr',
            'xadd', 'xchg', 'xlat', 'xor', 'xorps'
            /* spellchecker: enable */
        ].join('|') + ')(?:$|\\s)')
    };

    function conditional(mnemonic: string) {
        /* spellchecker: disable */
        return mnemonic + '(?:n?[abgl]e?|n?[ceosz]|np|p[eo]?)';
        /* spellchecker: enable */
    }

    return {
        startState: function () {
            return {};
        },

        token: function (stream) {
            if (stream.eatSpace()) {
                return null;
            }

            if (stream.eat(';')) {
                stream.skipToEnd();
                return 'comment';
            }

            if (stream.match(/\w+:/)) {
                return 'tag';
            }

            for (const key in grammar) {
                if (stream.match(grammar[key as keyof typeof grammar])) {
                    return key;
                }
            }

            stream.match(/\S+/);
            return null;
        }
    };
});

CodeMirror.defineMIME('text/x-asm', 'asm');