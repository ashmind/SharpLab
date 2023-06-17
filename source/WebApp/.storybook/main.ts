import { StorybookConfig } from '@storybook/react-webpack5';
import path from 'path';
import { StatsWriterPlugin } from 'webpack-stats-plugin';

export default {
  stories: ["../app/**/*.stories.tsx"],
  addons: ["@storybook/addon-links", "@storybook/addon-essentials", "@storybook/addon-interactions"],
  framework: {
    name: "@storybook/react-webpack5",
    options: {}
  },
  webpackFinal: async config => {
    // Terser fails with "__spreadProps is not defined"
    config.optimization ??= {};
    config.optimization.minimize = false;

    config.resolve ??= {};
    config.resolve.symlinks = false;
    config.resolve.alias ??= {};
    config.resolve.alias[path.resolve(__dirname, '../app/features/roslyn-branches/internal/branchesPromise.ts')] = path.resolve(__dirname, '__mocks__/branchesPromise.ts');

    config.module ??= {};
    config.module.rules ??= [];
    config.module.rules.push({
      test: /\.less$/,
      use: ["style-loader", "css-loader", "less-loader"]
    });
    config.plugins ??= [];
    config.plugins.push(new StatsWriterPlugin({
      filename: "stats.json",
      fields: null
    }));
    return config;
  }
} satisfies StorybookConfig;