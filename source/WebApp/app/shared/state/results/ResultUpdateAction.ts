import type { UpdateResult } from '../../../../ts/types/results';
import type { CachedUpdateResult } from '../../../features/result-cache/types';
import type { TargetName } from '../../targets';

export type ResultUpdateAction = {
    type: 'updateResult';
    updateResult: UpdateResult;
    target: TargetName;
} | {
    type: 'cachedResult';
    updateResult: CachedUpdateResult;
    target: TargetName;
} | {
    type: 'serverError';
    message: string;
};