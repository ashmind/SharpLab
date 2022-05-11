import type { LanguageName } from '../../app/shared/languages';
import type { Branch } from '../../app/shared/types/Branch';
import type { TargetName } from '../helpers/targets';

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