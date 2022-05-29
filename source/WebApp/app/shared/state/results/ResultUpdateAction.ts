import type { CachedUpdateResult } from '../../../features/result-cache/types';
import type { UpdateResult } from '../../resultTypes';
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