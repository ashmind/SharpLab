/* global require:false */

'use strict';
const fs = require('fs');
const gulp = require('gulp');
const g = require('gulp-load-plugins')();
const webpack = require('webpack-stream');
const assign = require('object-assign');
const pipe = require('multipipe');

gulp.task('less', () => {
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

gulp.task('js', () => {
    var config = require('./webpack.config.js');
    assign(config.output, { filename: 'app.min.js' });
    return gulp
        .src('./js/app.js')
        .pipe(webpack(config))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('favicons', () => {
    return gulp
        .src('./favicon.svg')
        .pipe(g.mirror(
          g.noop(),
          pipe(g.svg2png({ width:  16, height:  16 }), g.rename({suffix:'-16'})),
          pipe(g.svg2png({ width:  32, height:  32 }), g.rename({suffix:'-32'})),
          pipe(g.svg2png({ width:  64, height:  64 }), g.rename({suffix:'-64'})),
          pipe(g.svg2png({ width:  96, height:  96 }), g.rename({suffix:'-96'})),
          pipe(g.svg2png({ width: 128, height: 128 }), g.rename({suffix:'-128'})),
          pipe(g.svg2png({ width: 196, height: 196 }), g.rename({suffix:'-196'})),
          pipe(g.svg2png({ width: 256, height: 256 }), g.rename({suffix:'-256'}))
        ))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('html', () => {
    const faviconSvg = fs.readFileSync('favicon.svg', 'utf8');
    // http://codepen.io/jakob-e/pen/doMoML
    const faviconSvgUrlSafe = faviconSvg
        .replace(/"/g, '\'')
        .replace(/%/g, '%25')
        .replace(/#/g, '%23')
        .replace(/{/g, '%7B')
        .replace(/}/g, '%7D')
        .replace(/</g, '%3C')
        .replace(/>/g, '%3E')
        .replace(/\s+/g,' ');

    return gulp
        .src('./index.html')
        .pipe(g.htmlReplace({ js: 'app.min.js', css: 'app.min.css' }))
        .pipe(g.replace('{build:favicon-svg}', faviconSvgUrlSafe))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', () => {
    gulp.watch('less/**/*.less', ['less']);
    gulp.watch('js/**/*.js', ['js']);
    gulp.watch('favicon*.svg', ['favicons']);
    gulp.watch('index.html', ['html']);
});

gulp.task('default', ['less', 'js', 'favicons', 'html']);