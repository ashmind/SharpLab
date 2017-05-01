const path = require('path');
const nodeResolve = require('rollup-plugin-node-resolve');
const commonjs = require('rollup-plugin-commonjs');

const plugins = [
    {
        name: 'rollup-plugin-adhoc-resolve-vue',
        resolveId: id => (id === 'vue') ? path.resolve('./node_modules/vue/dist/vue.min.js') : null
    },
    nodeResolve({
        browser: true
    }),
    commonjs({
        include: ['node_modules/**', 'js/ui/codemirror/**']
    })
];

module.exports = {
    entry: 'js/app.js',
    plugins: plugins,
    dest: 'wwwroot/app.rollup-test.js',
    format: 'iife',
    sourceMap: true
};