import type { RawOptions } from './raw-options';

export interface Gist {
    readonly id: string;
    readonly name: string;
    readonly url: string;
    readonly options: RawOptions;
    readonly code: string;
}