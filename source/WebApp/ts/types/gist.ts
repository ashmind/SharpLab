import type { LanguageName } from '../helpers/languages';
import type { TargetName } from '../helpers/targets';

export type Gist = {
    readonly id: string;
    readonly name: string;
    readonly url: string;
    readonly options: {
        language: LanguageName;
        target: TargetName;
        release: boolean;
        branchId: string | null;
    };
    readonly code: string;
};