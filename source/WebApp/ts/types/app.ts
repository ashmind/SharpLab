import type { Branch } from '../../app/features/roslyn-branches/types';
import type { LanguageName } from '../../app/shared/languages';
import type { TargetName } from '../../app/shared/targets';

export interface AppOptions {
    language: LanguageName;
    target: TargetName;
    release: boolean;
    branch: Branch | null;
}

export interface AppStatus {
    online: boolean;
    error: boolean;
    color: '#4684ee'|'#dc3912'|'#aaa';
}