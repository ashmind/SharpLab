/* globals module:false, require:false */
const webpack = require('webpack');

module.exports = {
    externals: {
        jquery: 'jQuery'
    },
    devtool: 'source-map',    
    plugins: [
        //new webpack.optimize.UglifyJsPlugin()
        /*new webpack.SourceMapDevToolPlugin({
            filename: 'app.min.js.map'
        })*/
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