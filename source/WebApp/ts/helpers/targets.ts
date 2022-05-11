import { LANGUAGE_CSHARP, LANGUAGE_VB } from '../../app/shared/languages';

export const targets = Object.freeze({
    csharp:  LANGUAGE_CSHARP,
    vb:      LANGUAGE_VB, // no longer supported, just a placeholder
    il:      'IL',
    asm:     'JIT ASM',
    ast:     'AST',
    run:     'Run',
    verify:  'Verify',
    explain: 'Explain'
} as const);

export type TargetName = typeof targets[keyof typeof targets];
export type TargetLanguageName = typeof targets.csharp | typeof targets.vb | typeof targets.il | typeof targets.asm;