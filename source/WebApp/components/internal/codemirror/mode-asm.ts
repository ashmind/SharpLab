// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE
import CodeMirror from 'codemirror';
import instructionsRegex from './mode-asm-instructions';

CodeMirror.defineMode('asm', () => {
    const grammar = {
        builtin: instructionsRegex
    };
    const specifiers = [ 'byte', 'dword', 'ptr', 'qword', 'short', 'tbyte', 'word' ];
    const registers = [ 'ah', 'al', 'ax', 'bh', 'bl', 'bp', 'bpl', 'bx', 'ch', 'cl', 'cx', 'dh', 'di', 'dil',
        'dl', 'dx', 'eax', 'ebp', 'ebx', 'ecx', 'edi', 'edx', 'esi', 'esp', 'rax', 'rbp', 'rbx', 'rcx', 'rdi',
        'rdx', 'rsi', 'rsp', 'r8', 'r9', 'r10', 'r11', 'r12', 'r13', 'r14', 'r15', 'r8b', 'r9b', 'r10b', 'r11b',
        'r13b', 'r14b', 'r15b', 'r8d', 'r9d', 'r10d', 'r11d', 'r12d', 'r13d', 'r14d', 'r15d', 'r8w', 'r9w',
        'r10w', 'r11w', 'r12w', 'r13w', 'r14w', 'r15w' ];

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