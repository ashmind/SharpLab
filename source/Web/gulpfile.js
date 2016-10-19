/* global require:false */

'use strict';
var gulp = require('gulp');
var g = require('gulp-load-plugins')();
var webpack = require('webpack-stream');
var assign = require('object-assign');
var pipe = require('multipipe');

gulp.task('less', function () {
    return gulp
        .src('./less/app.less')
        // this doesn't really work properly, e.g. https://github.com/ai/autoprefixer-core/issues/27
        .pipe(g.sourcemaps.init())
        .pipe(g.less())
        .pipe(g.autoprefixer({ cascade: false }))
        .pipe(g.cleanCss({ processImport: false }))
        .pipe(g.rename('app.min.css'))
        .pipe(g.sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('js', function () {
    var config = require('./webpack.config.js');
    assign(config.output, { filename: 'app.min.js' });
    return gulp
        .src('./js/app.js')
        .pipe(webpack(config))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('favicon', function () {
    return gulp
        .src('./favicon.svg')
        .pipe(g.mirror(
          g.rename('favicon.svg'),
          pipe(g.svg2png({ width:  16, height:  16 }), g.rename('favicon-16.png')),
          pipe(g.svg2png({ width:  32, height:  32 }), g.rename('favicon-32.png')),
          pipe(g.svg2png({ width:  64, height:  64 }), g.rename('favicon-64.png')),
          pipe(g.svg2png({ width:  96, height:  96 }), g.rename('favicon-64.png')),
          pipe(g.svg2png({ width: 128, height: 128 }), g.rename('favicon-128.png')),
          pipe(g.svg2png({ width: 196, height: 196 }), g.rename('favicon-196.png')),
          pipe(g.svg2png({ width: 256, height: 256 }), g.rename('favicon-256.png'))
        ))
        .pipe(gulp.dest('wwwroot/favicons'));
});

gulp.task('html', function () {
    return gulp
        .src('./index.html')
        .pipe(g.htmlReplace({ js: 'app.min.js', css: 'app.min.css' }))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', ['default'], function () {
    gulp.watch('less/**/*.less', ['less']);
    gulp.watch('js/**/*.js', ['js']);
    gulp.watch('favicon.svg', ['favicon']);
    gulp.watch('index.html', ['html']);
});

gulp.task('default', ['less', 'js', 'favicon', 'html']);