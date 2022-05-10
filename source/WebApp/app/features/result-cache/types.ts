import type { NonErrorUpdateResult } from '../../../ts/types/results';

export type MaybeCached<T> = T & { cached?: { date: Date } };
export type CachedUpdateResult = NonErrorUpdateResult & { cached: { date: Date } };