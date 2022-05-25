const path = require('path');

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
        return config;
    }
}