import type { AppOptions } from './app';

export interface Gist {
    id: string;
    name: string;
    url: string;
    options: AppOptions;
    code: string;
}