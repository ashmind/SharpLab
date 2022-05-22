import type { NonErrorUpdateResult } from '../../shared/resultTypes';

export type MaybeCached<T> = T & { cached?: { date: Date } };
export type CachedUpdateResult = NonErrorUpdateResult & { cached: { date: Date } };