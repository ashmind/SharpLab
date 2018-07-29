const path = require('path');
const nodeResolve = require('rollup-plugin-node-resolve');
const commonjs = require('rollup-plugin-commonjs');

const production = process.env.NODE_ENV === 'production';

const plugins = [
    commonjs({
        include: ['node_modules/**', 'js/ui/codemirror/**', 'js/ui/helpers/**']
    }),
    {
        name: 'rollup-plugin-adhoc-resolve-vue',
        resolveId: id => (id === 'vue') ? path.resolve(`./node_modules/vue/dist/vue${production?'.min':''}.js`) : null
    },
    nodeResolve({
        browser: true
    })
];

module.exports = {
    plugins: plugins,
    output: {
        format: 'iife',
        sourcemap: true,
    },

    // test only, gulp does not use these
    entry: 'js/app.js',
    dest: 'wwwroot/app.rollup-test.js'
};