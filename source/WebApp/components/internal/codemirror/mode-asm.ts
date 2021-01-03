// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE
import CodeMirror from 'codemirror';
import instructionsRegex from './mode-asm-instructions';

CodeMirror.defineMode('asm', () => {
    const grammar = {
        builtin: instructionsRegex
    };

    return {
        startState() {
            return {};
        },

        token(stream) {
            if (stream.eatSpace()) {
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