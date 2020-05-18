import type { AppOptions } from './app';

export interface Gist {
    readonly id: string;
    readonly name: string;
    readonly url: string;
    readonly options: AppOptions;
    readonly code: string;
}