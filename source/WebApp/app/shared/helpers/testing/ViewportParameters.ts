import type { INITIAL_VIEWPORTS } from '@storybook/addon-viewport';

export type ViewportParameters = {
    viewports: typeof INITIAL_VIEWPORTS;
    defaultViewport: string;
};