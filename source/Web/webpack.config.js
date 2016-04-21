/* globals module:false, require:false, process:false */
var webpack = require('webpack');

var plugins = [];
if (process.env.NODE_ENV === 'production') {
    plugins.push(new webpack.optimize.UglifyJsPlugin());
}

module.exports = {
    externals: {
        jquery: 'jQuery'
    },
    devtool: 'source-map',    
    plugins: plugins,
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