const path = require('path');
const nodeResolve = require('rollup-plugin-node-resolve');
const commonjs = require('rollup-plugin-commonjs');

const production = process.env.NODE_ENV === 'production';

const plugins = [
    {
        name: 'rollup-plugin-adhoc-resolve-vue',
        resolveId: id => (id === 'vue') ? path.resolve(`./node_modules/vue/dist/vue${production?'.min':''}.js`) : null
    },
    nodeResolve({
        browser: true
    }),
    commonjs({
        include: ['node_modules/**', 'js/ui/codemirror/**', 'js/ui/helpers/**']
    })
];

module.exports = {
    plugins: plugins,
    format: 'iife',
    sourceMap: true,

    // test only, gulp does not use these
    entry: 'js/app.js',
    dest: 'wwwroot/app.rollup-test.js'
};