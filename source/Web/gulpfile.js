"use strict";
const gulp = require('gulp');
const plumber = require('gulp-plumber');
const uglify = require('gulp-uglify');
const g = require('gulp-load-plugins')();
const htmlreplace = require('gulp-html-replace');
const webpack = require('webpack-stream');
const assign = require('object-assign');

gulp.task('less', function () {
    return gulp
        .src('./less/app.less')
        .pipe(g.sourcemaps.init())
        .pipe(g.less())
        .pipe(g.sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('js', function () {
    let config = require('./webpack.config.js');
    assign(config.output, {
        filename: "app.min.js"
    });
    return gulp
        .src('./js/app.js')
        .pipe(webpack(config))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('html', function () {
    return gulp
        .src('./index.html')
        .pipe(htmlreplace({ js: 'app.min.js', css: 'app.css' }))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', ['default'], function () {
    gulp.watch('less/**/*.less', ['less']);
    gulp.watch('js/**/*.js', ['js']);
    gulp.watch('index.html', ['html']);
});

gulp.task('default', ['less', 'js', 'html']);