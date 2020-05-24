import type { AppOptions } from './app';

export type RawOptions = Omit<AppOptions, 'branch'> & { branchId: string|null };