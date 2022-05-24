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