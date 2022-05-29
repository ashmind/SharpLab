import type { LanguageName } from '../../../../shared/languages';
import type { TargetName } from '../../../../shared/targets';

const exact = Symbol('Options were validated and confirmed not to include any extra keys');

export type OptionsData = {
    language: LanguageName;
    branchId: string | null;
    target: TargetName;
    release: boolean;
};

export type ExactOptionsData = OptionsData & {
    [exact]: true;
};

export const toOptionsData = (
    language: LanguageName,
    branch: { id: string } | null,
    target: TargetName,
    release: boolean
): ExactOptionsData => ({
    [exact]: true,
    language,
    release,
    target,
    branchId: branch?.id ?? null
});