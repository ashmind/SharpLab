import { DecoratorFn } from '@storybook/react';
import React from 'react';
import { RecoilRoot } from 'recoil';
import { ViewportParameters } from '../app/shared/helpers/testing/viewportParameters';
import '../less/app.less';

export const loaders = [
    () => document.fonts.ready
];

export const decorators: DecoratorFn[] = [
    (Story, props) => {
        // https://github.com/storybookjs/test-runner/issues/97#issuecomment-1134419035
        (window as unknown as { STORY_VIEWPORT_PARAMETERS: ViewportParameters }).STORY_VIEWPORT_PARAMETERS = props?.parameters?.viewport;
        return <Story />;
    },
    (Story) => <RecoilRoot>
        <Story />
    </RecoilRoot>
];

export const parameters = {
  actions: { argTypesRegex: "^on[A-Z].*" },
  controls: {
    matchers: {
      color: /(background|color)$/i,
      date: /Date$/,
    },
  },
}