// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE
import CodeMirror from 'codemirror';
import instructionsRegex from './mode-asm-instructions';

CodeMirror.defineMode('asm', () => {
    const grammar = {
        builtin: instructionsRegex
    };
    const specifiers = [ 'byte', 'dword', 'ptr', 'qword', 'short', 'tbyte', 'word' ];

    // In order to match longest possible register name, this array is pre-sorted using naturalSort function:
    // https://github.com/Bill4Time/javascript-natural-sort/blob/master/naturalSort.js
    const registers = [ 'rsp', 'rsi', 'rdx', 'rdi', 'rcx', 'rbx', 'rbp', 'rax', 'r15w', 'r15d', 'r15b', 'r15',
        'r14w', 'r14d', 'r14b', 'r14', 'r13w', 'r13d', 'r13b', 'r13', 'r12w', 'r12d', 'r12', 'r11w', 'r11d', 'r11b',
        'r11', 'r10w', 'r10d', 'r10b', 'r10', 'r9w', 'r9d', 'r9b', 'r9', 'r8w', 'r8d', 'r8b', 'r8', 'esp', 'esi',
        'edx', 'edi', 'ecx', 'ebx', 'ebp', 'eax', 'dx', 'dl', 'dil', 'di', 'dh', 'cx', 'cl', 'ch', 'bx', 'bpl',
        'bp', 'bl', 'bh', 'ax', 'al', 'ah' ];

    return {
        startState() {
            return {};
        },

        token(stream) {
            if (stream.eatSpace() || stream.eat('[') || stream.eat(']')) {
                return null;
            }

            if (stream.eat(';')) {
                stream.skipToEnd();
                return 'comment';
            }

            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            if (stream.match(/\w+:/)) {
                return 'tag';
            }

            for (const index in specifiers) {
                if (stream.match(specifiers[index], true, true)) {
                    return 'keyword';
                }
            }

            for (const index in registers) {
                if (stream.match(registers[index], true, true)) {
                    return 'type';
                }
            }

            for (const key in grammar) {
                // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
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