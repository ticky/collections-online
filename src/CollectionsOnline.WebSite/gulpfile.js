/// <vs BeforeBuild='build-css' SolutionOpened='watch' />
// Steps to build
// 1. install node: http://nodejs.org/download/
// 2. install gulp globally: npm install -g gulp
// 3. install ruby http://rubyinstaller.org/downloads/
// 4. install sass: gem install sass
// 5. install modules from web project: npm install
// 6. use task runner explorer or execute gulpfile: gulp
'use strict';

var gulp = require('gulp');
var sass = require('gulp-ruby-sass');

var source = require('vinyl-source-stream');
var browserify = require('browserify');

var filePaths = {
  css: { src: './content/scss/styles.scss', dest: './content/css' }
}

gulp.task('build-css', function () {
    return gulp.src(filePaths.css.src)
        .pipe(sass({ noCache: true, sourcemap: true }))
        .pipe(gulp.dest(filePaths.css.dest));
});

// Watch Task.
gulp.task('watch', function () {
  gulp.watch('./content/scss/**/*.scss', ['build-css']);
});

gulp.task('default', ['build-css']);

gulp.task('browserify', function () {
  return browserify('./content/js/app/app.js')
     .bundle()
     .pipe(source('bundle.js'))
     .pipe(gulp.dest('./content/js/'));
});