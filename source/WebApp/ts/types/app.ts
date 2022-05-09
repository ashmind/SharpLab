import type { TargetName } from '../helpers/targets';
import type { LanguageName } from '../helpers/languages';
import type { Branch } from './branch';

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