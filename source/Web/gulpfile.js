/* global require:false */

'use strict';
const gulp = require('gulp');
const g = require('gulp-load-plugins')();
const webpack = require('webpack-stream');
const assign = require('object-assign');
const pipe = require('multipipe');

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

gulp.task('favicons', function () {
    function rename(suffix, extension) {
        return g.rename(path => {
            path.extension = extension || path.extension;
            const parts = path.basename.split('-');
            if (parts.length > 1) {
                path.dirname = parts[1];
                path.basename = parts[0];
            }
            path.basename += suffix || '';
            return;
        });
    }

    return gulp
        .src('./favicon*.svg')
        .pipe(g.mirror(
          rename(),
          pipe(g.svg2png({ width:  16, height:  16 }), rename('-16',  'png')),
          pipe(g.svg2png({ width:  32, height:  32 }), rename('-32',  'png')),
          pipe(g.svg2png({ width:  64, height:  64 }), rename('-64',  'png')),
          pipe(g.svg2png({ width:  96, height:  96 }), rename('-96',  'png')),
          pipe(g.svg2png({ width: 128, height: 128 }), rename('-128', 'png')),
          pipe(g.svg2png({ width: 196, height: 196 }), rename('-196', 'png')),
          pipe(g.svg2png({ width: 256, height: 256 }), rename('-256', 'png'))
        ))
        .pipe(gulp.dest('wwwroot/favicons'));
});

gulp.task('html', function () {
    return gulp
        .src('./index.html')
        .pipe(g.htmlReplace({ js: 'app.min.js', css: 'app.min.css' }))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', function () {
    gulp.watch('less/**/*.less', ['less']);
    gulp.watch('js/**/*.js', ['js']);
    gulp.watch('favicon*.svg', ['favicons']);
    gulp.watch('index.html', ['html']);
});

gulp.task('default', ['less', 'js', 'favicons', 'html']);