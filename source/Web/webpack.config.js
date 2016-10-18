/* globals module:false, require:false */
var webpack = require('webpack');
var path = require('path');

module.exports = {
    externals: {
        jquery: 'jQuery'
    },
    resolve: {
        root: path.resolve('./js')
    },
    devtool: 'source-map',
    plugins: [
        new webpack.optimize.UglifyJsPlugin()
    ],
    entry: [
        './js/app.js'
    ],
    module: {
        loaders: [{
            test: /\.js$/,
            exclude: /node_modules/,
            loader: 'babel',
            query: {
                plugins: [
                    'transform-es2015-modules-commonjs',
                    'transform-async-to-generator'
                ]
            }
        }]
    },
    output: {
        devtoolModuleFilenameTemplate: '[resource-path]'
    }
};