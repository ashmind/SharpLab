const path = require('path');
const { StatsWriterPlugin } = require("webpack-stats-plugin");

module.exports = {
    "stories": [
        "../app/**/*.stories.tsx"
    ],
    "addons": [
        "@storybook/addon-links",
        "@storybook/addon-essentials",
        "@storybook/addon-interactions"
    ],
    "framework": "@storybook/react",
    core: {
        builder: 'webpack5',
    },
    webpackFinal: async (config) => {
        config.resolve.symlinks = false;
        config.resolve.alias[
            path.resolve(__dirname, '../app/features/roslyn-branches/internal/branchesPromise.ts')
        ] = path.resolve(__dirname, '__mocks__/branchesPromise.ts');
        config.module.rules.push({
            test: /\.less$/,
            use: [
                "style-loader",
                "css-loader",
                "less-loader",
            ]
        });
        config.plugins.push(new StatsWriterPlugin({
            filename: "stats.json",
            fields: null
        }));
        return config;
    }
}