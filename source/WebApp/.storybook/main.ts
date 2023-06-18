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
    webpackFinal: async config => ({
        ...config,
        optimization: {
            ...(config.optimization ?? {}),
            // Terser fails with "__spreadProps is not defined"
            minimize: false
        },
        resolve: {
            ...(config.resolve ?? {}),
            symlinks: false,
            alias: {
                ...(config.resolve?.alias ?? {}),
                [path.resolve(__dirname, '../app/features/roslyn-branches/internal/branchesPromise.ts')]:
                    path.resolve(__dirname, '__mocks__/branchesPromise.ts')
            }
        },
        module: {
            ...(config.module ?? {}),
            rules: [
                ...(config.module?.rules ?? []),
                {
                    test: /\.less$/,
                    use: ["style-loader", "css-loader", "less-loader"]
                }
            ]
        },
        plugins: [
            ...(config.plugins ?? []),
            new StatsWriterPlugin({
                filename: "stats.json",
                fields: null
            })
        ]
    })
} satisfies StorybookConfig;