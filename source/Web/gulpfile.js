var gulp = require('gulp');
var concat = require('gulp-concat');
var plumber = require('gulp-plumber');
var uglify = require('gulp-uglify');
var less = require('gulp-less');
var htmlreplace = require('gulp-html-replace');

gulp.task('less', function () {
  return gulp
    .src('./legacy/app.less')
    .pipe(less())
    .pipe(gulp.dest('wwwroot'));
});

gulp.task('js', function () {
  return gulp
    .src(['./legacy/external/**/*.js', './legacy/app.js', './legacy/**/*.js'])
    .pipe(concat('app.js'))
    .pipe(uglify())
    .pipe(gulp.dest('wwwroot'));
});

gulp.task('html', function () {
  return gulp
    .src('./legacy/index.html')
    .pipe(htmlreplace({ js: 'app.js', css: 'app.css' }))
    .pipe(gulp.dest('wwwroot'));
});

gulp.task('watch', function () {
    gulp.watch('**/*.less', ['less']);
});

gulp.task('default', ['less', 'js', 'html']);