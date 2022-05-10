import { createContext } from 'react';
import type { TargetName } from '../../../ts/helpers/targets';
import type { UpdateResult, ParsedResult } from '../../../ts/types/results';
import type { CachedUpdateResult, MaybeCached } from '../../features/result-cache/types';

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

export const ResultContext = createContext<readonly [
    result: MaybeCached<ParsedResult> | undefined,
    dispatchResultAction: (action: ResultUpdateAction) => void
// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
]>(null!);