var gulp = require('gulp');
var plumber = require('gulp-plumber');
var uglify = require('gulp-uglify');
var less = require('gulp-less');
var htmlreplace = require('gulp-html-replace');
var webpack = require('webpack-stream');
var assign = require('object-assign');

gulp.task('less', function () {
    return gulp
        .src('./legacy/app.less')
        .pipe(less())
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('js', function () {
    var config = require('./webpack.config.js');

    return gulp
        .src('./js/app.js')
        .pipe(webpack(assign({}, config, {
            output: { filename: "app.min.js" }
         })))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('html', function () {
    return gulp
        .src('./index.html')
        .pipe(htmlreplace({ js: 'app.min.js', css: 'app.css' }))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', ['default'], function () {
    gulp.watch('legacy/**/*.less', ['less']);
    gulp.watch('js/**/*.js', ['js']);
    gulp.watch('index.html', ['html']);
});

gulp.task('default', ['less', 'js', 'html']);