import features from './features.js';
import languages from './languages.js';

const targets = {
    csharp: languages.csharp,
    vb:     languages.vb,
    il:     'IL',
    asm:    'JIT ASM',
    ast:    'AST'
};
if (features.run)
    targets.run = 'Run';
export default Object.freeze(targets);