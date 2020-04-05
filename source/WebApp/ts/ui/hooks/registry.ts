import type { AppVue } from '../../types/app';

export type Hooks = {
    ready: Array<(app: AppVue) => void>;
};

export const allHooks = (Object.freeze({
    main: {
        ready: []
    }
} as {
    main: Hooks;
    footer?: Hooks;
}));