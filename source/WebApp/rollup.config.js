/* eslint-disable @typescript-eslint/no-unsafe-call */
/* eslint-disable @typescript-eslint/ban-ts-ignore */
/* eslint-disable no-process-env */

import path from 'path';
// @ts-ignore
import pluginBabel from 'rollup-plugin-babel';
import pluginNodeResolve from '@rollup/plugin-node-resolve';
import pluginCommonJS from 'rollup-plugin-commonjs';
import pluginTypeScript from '@rollup/plugin-typescript';
// @ts-ignore
import { terser } from 'rollup-plugin-terser';

export default {
    input: './ts/app.ts',
    plugins: [
        {
            name: 'rollup-plugin-adhoc-resolve-vue',
            // @ts-ignore
            resolveId: id => (id === 'vue')
                ? path.resolve(`./node_modules/vue/dist/vue${process.env.NODE_ENV === 'production' ? '.min' : ''}.js`)
                : null
        },
        pluginNodeResolve(),
        pluginTypeScript({ include: ['ts/**/*.ts', 'components/**/*.ts'] }),
        pluginBabel({
            extensions: ['.js', '.ts'],
            presets: [['@babel/preset-env', { loose: true }]]
        }),
        pluginCommonJS({
            include: [
                'node_modules/**'
            ]
        }),
        terser()
    ],
    output: {
        format: 'iife',
        file: './wwwroot/app.min.js',
        sourcemap: true
    }
};