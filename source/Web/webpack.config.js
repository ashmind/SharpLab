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
        'regenerator/runtime',
        './js/app.js'
    ],
    module: {
        loaders: [{
            test: /\.js$/,
            exclude: /node_modules/,
            loader: 'babel',
            query: {
                presets: ['es2015'],
                plugins: ['syntax-async-functions', 'transform-regenerator']
            }
        }]
    },
    output: {
        devtoolModuleFilenameTemplate: '[resource-path]'
    }
};