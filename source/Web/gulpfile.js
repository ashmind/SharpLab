/* global require:false */

'use strict';
var gulp = require('gulp');
var g = require('gulp-load-plugins')();
var webpack = require('webpack-stream');
var assign = require('object-assign');

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

gulp.task('html', function () {
    return gulp
        .src('./index.html')
        .pipe(g.htmlReplace({ js: 'app.min.js', css: 'app.min.css' }))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', ['default'], function () {
    gulp.watch('less/**/*.less', ['less']);
    gulp.watch('js/**/*.js', ['js']);
    gulp.watch('index.html', ['html']);
});

gulp.task('default', ['less', 'js', 'html']);