/* globals module:false, require:false, process:false */
var webpack = require('webpack');
module.exports = {
    externals: {
        jquery: 'jQuery'
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