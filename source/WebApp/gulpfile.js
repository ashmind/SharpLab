/* global require:false, process:false */
/* eslint-disable arrow-body-style, import/no-commonjs */

'use strict';
const fs = require('fs');
const md5File = require('md5-file');
const gulp = require('gulp');
const g = require('gulp-load-plugins')();
const pipe = require('multipipe');

// ReSharper disable once UndeclaredGlobalVariableUsing
const production = process.env.NODE_ENV === 'production';

gulp.task('less', () => {
    return gulp
        .src('./less/app.less')
        .pipe(g.plumber())
        // this doesn't really work properly, e.g. https://github.com/ai/autoprefixer-core/issues/27
        .pipe(g.sourcemaps.init())
        .pipe(g.less())
        .pipe(g.autoprefixer({ cascade: false }))
        //.pipe(g.cleanCss({ processImport: false }))
        .pipe(g.rename('app.min.css'))
        .pipe(g.sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('js', () => {
    const config = require('./rollup.config.js');
    delete config.entry;
    delete config.dest;
    return gulp
        .src('./js/app.js')
        .pipe(g.plumber())
        .pipe(g.sourcemaps.init())
        .pipe(g.betterRollup(config, config))
        .pipe(g.if(production, g.babili({ comments: false })))
        .pipe(g.rename('app.min.js'))
        .pipe(g.sourcemaps.write('.'))
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

gulp.task('html-only', () => {
    const roslynVersion = getRoslynVersion();
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
    const jsHash  = md5File.sync('wwwroot/app.min.js');
    const cssHash = md5File.sync('wwwroot/app.min.css');

    return gulp
        .src('./index.html')
        .pipe(g.htmlReplace({ js: 'app.min.js?' + jsHash, css: 'app.min.css?' + cssHash })) // eslint-disable-line prefer-template
        .pipe(g.replace('{build:favicon-svg}', faviconSvgUrlSafe))
        .pipe(g.replace('{build:roslyn-version}', roslynVersion))
        .pipe(gulp.dest('wwwroot'));
});

gulp.task('html', ['js', 'less', 'html-only']);

function getRoslynVersion() {
    const assetsJson = JSON.parse(fs.readFileSync('../Server/obj/project.assets.json', 'utf8'));
    for (const key in assetsJson.libraries) {
        const match = key.match(/^Microsoft\.CodeAnalysis\.Common\/(.+)$/);
        if (match)
            return match[1];
    }
    return null;
}

gulp.task('watch', ['default'], () => {
    gulp.watch('less/**/*.*', ['less', 'html-only']);
    gulp.watch('js/**/*.js', ['js', 'html-only']);
    gulp.watch('favicon*.svg', ['favicons']);
    gulp.watch('index.html', ['html-only']);
});

gulp.task('default', ['less', 'js', 'favicons', 'html']);