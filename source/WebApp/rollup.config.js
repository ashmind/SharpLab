/* eslint-disable @typescript-eslint/no-unsafe-call */
/* eslint-disable no-process-env */

import path from 'path';
// @ts-expect-error (no typings)
import pluginBabel from 'rollup-plugin-babel';
import pluginNodeResolve from '@rollup/plugin-node-resolve';
import pluginCommonJS from 'rollup-plugin-commonjs';
import pluginTypeScript from '@rollup/plugin-typescript';
import { terser } from 'rollup-plugin-terser';

export default {
    input: './ts/app.ts',
    preserveSymlinks: true,
    plugins: [
        {
            name: 'rollup-plugin-adhoc-resolve-vue',
            /** @param id {string} */
            resolveId: id => (id === 'vue')
                ? path.resolve(`./node_modules/vue/dist/vue${process.env.NODE_ENV === 'ci' ? '.min' : ''}.js`)
                : null
        },
        pluginNodeResolve(),
        pluginTypeScript({ include: ['ts/**/*.ts', 'components/**/*.ts'] }),
        pluginBabel({
            extensions: ['.js', '.ts'],
            presets: [['@babel/preset-env', { loose: true }]]
        }),
        pluginCommonJS({
            include: ['node_modules/**']
        }),
        ...(process.env.NODE_ENV === 'ci' ? [terser()] : [])
    ],
    output: {
        format: 'iife',
        sourcemap: true
    }
};