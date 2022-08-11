import { LANGUAGE_CSHARP, LANGUAGE_VB } from '../../app/shared/languages';

export const TARGET_CSHARP = LANGUAGE_CSHARP;
export const TARGET_VB = LANGUAGE_VB; // no longer supported, just a placeholder
export const TARGET_IL = 'IL';
export const TARGET_ASM = 'JIT ASM';
export const TARGET_AST = 'AST';
export const TARGET_VERIFY = 'Verify';
export const TARGET_EXPLAIN = 'Explain';
export const TARGET_RUN = 'Run';
export const TARGET_RUN_IL = 'Run IL';

export type TargetNameTuple = [
    typeof TARGET_CSHARP,
    typeof TARGET_VB,
    typeof TARGET_IL,
    typeof TARGET_ASM,
    typeof TARGET_AST,
    typeof TARGET_VERIFY,
    typeof TARGET_EXPLAIN,
    typeof TARGET_RUN,
    typeof TARGET_RUN_IL
];

export type TargetName = TargetNameTuple[number];
export type TargetLanguageName = typeof TARGET_CSHARP
                               | typeof TARGET_VB
                               | typeof TARGET_IL
                               | typeof TARGET_ASM;