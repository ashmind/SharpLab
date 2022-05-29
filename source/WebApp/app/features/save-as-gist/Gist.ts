import type { LanguageName } from '../../shared/languages';
import type { TargetName } from '../../shared/targets';

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