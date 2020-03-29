import rollupPluginCommonJS from 'rollup-plugin-commonjs';
import pluginTypeScript from '@rollup/plugin-typescript';
import { terser } from 'rollup-plugin-terser';

export default {
    // https://github.com/rollup/rollup/issues/2473
    treeshake: false,
    input: './ts/app.ts',
    plugins: [
        rollupPluginCommonJS({
            include: [
                'node_modules/**'
            ]
        }),/*
        {
            name: 'rollup-plugin-adhoc-resolve-vue',
            resolveId: id => (id === 'vue') ? path.resolve(`./node_modules/vue/dist/vue${production?'.min':''}.js`) : null
        },*/
        pluginTypeScript(),
        ...(process.env.NODE_ENV === 'production' ? [terser()] : [])
    ],
    output: {
        format: 'iife',
        file: './wwwroot/app.min.js',
        sourcemap: true
    }
};