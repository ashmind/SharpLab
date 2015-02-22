var gulp = require('gulp');
var watch = require('gulp-watch');
var plumber = require('gulp-plumber');
var less = require('gulp-less');

gulp.task('less', function () {
  return gulp
    .src('./app.less')
    .pipe(less())
    .pipe(gulp.dest('.'));
});

gulp.task('watch', function () {
    gulp.watch('**/*.less', ['less']);
});

gulp.task('default', ['less']);