/* eslint-disable no-process-env */
/* eslint-disable @typescript-eslint/restrict-template-expressions */
/* eslint-disable @typescript-eslint/restrict-plus-operands */
/* eslint-disable @typescript-eslint/no-unsafe-return */
/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/no-unsafe-call */
/* eslint-disable @typescript-eslint/ban-ts-ignore */
/* eslint-disable import/extensions */

// Common:
import path from 'path';
import url from 'url';
// @ts-ignore
import oldowan from 'oldowan';
import jetpack from 'fs-jetpack';
import execa from 'execa';
import md5File from 'md5-file/promise.js';
// CSS:
// @ts-ignore
import less from 'less';
import postcss from 'postcss';
// @ts-ignore
import autoprefixer from 'autoprefixer';
// @ts-ignore
import csso from 'postcss-csso';
// Favicons:
// @ts-ignore
import sharp from 'sharp';
// HTML:
// @ts-ignore
import htmlMinifier from 'html-minifier';

const dirname = path.dirname(url.fileURLToPath(import.meta.url));

const { task, tasks, run } = oldowan;
const outputRoot = `${dirname}/wwwroot`;

// @ts-ignore
const parallel = (...promises) => Promise.all(promises);

const paths = {
    from: {
        less: `${dirname}/less/app.less`,
        favicon: `${dirname}/favicon.svg`,
        html: `${dirname}/index.html`
    },
    to: {
        css: `${outputRoot}/app.min.css`,
        favicon: {
            svg: `${outputRoot}/favicon.svg`,
            png: `${outputRoot}/favicon-{size}.png`
        },
        html: `${outputRoot}/index.html`
    }
};

task('less', async () => {
    const content = await jetpack.readAsync(paths.from.less);
    let result = await less.render(content, {
        filename: paths.from.less,
        sourceMap: {
            sourceMapBasepath: `${dirname}`,
            outputSourceFiles: true
        }
    });
    result = await postcss([
        autoprefixer,
        csso({ restructure: false })
    ]).process(result.css, {
        from: paths.from.less,
        map: {
            inline: false,
            prev: result.map
        }
    });

    await parallel(
        jetpack.writeAsync(paths.to.css, result.css),
        jetpack.writeAsync(paths.to.css + '.map', result.map)
    );
}, { inputs: `${dirname}/less/**/*.less` });

task('tsLint', () => execa.command('eslint . --max-warnings 0 --ext .js,.ts', {
    preferLocal: true,
    stdout: process.stdout,
    stderr: process.stderr
}));

task('ts', async () => {
    await tasks.tsLint();
    await execa.command('rollup -c', {
        preferLocal: true,
        stdout: process.stdout,
        stderr: process.stderr
    });
}, {
    inputs: [
        `${dirname}/ts/**/*.ts`,
        `${dirname}/components/**/*.ts`,
        `${dirname}/package.json`
    ]
});

task('favicons', async () => {
    await jetpack.dirAsync(outputRoot);
    const pngGeneration = [16, 32, 64, 96, 128, 196, 256].map(size => {
        // https://github.com/lovell/sharp/issues/729
        const density = size > 128 ? Math.round(72 * size / 128) : 72;
        return sharp(paths.from.favicon, { density })
            .resize(size, size)
            .png()
            // @ts-ignore
            .toFile(paths.to.favicon.png.replace('{size}', size));
    });

    return parallel(
        jetpack.copyAsync(paths.from.favicon, paths.to.favicon.svg, { overwrite: true }),
        pngGeneration
    );
}, { inputs: paths.from.favicon });

task('html', async () => {
    const faviconDataUrl = await getFaviconDataUrl();
    const templates = await getCombinedTemplates();
    const [jsHash, cssHash] = await parallel(
        md5File('wwwroot/app.min.js'),
        md5File('wwwroot/app.min.css')
    );
    let html = await jetpack.readAsync(paths.from.html);
    // @ts-ignore
    html = html
        .replace('{build:js}', 'app.min.js?' + jsHash)
        .replace('{build:css}', 'app.min.css?' + cssHash)
        .replace('{build:templates}', templates)
        .replace('{build:favicon-svg}', faviconDataUrl);
    html = htmlMinifier.minify(html, { collapseWhitespace: true });
    // @ts-ignore
    await jetpack.writeAsync(paths.to.html, html);
}, {
    inputs: [
        `${dirname}/components/**/*.html`,
        paths.to.css,
        `${outputRoot}/app.min.js`,
        paths.from.html,
        paths.from.favicon
    ]
});

task('default', () => {
    const htmlAll = async () => {
        await parallel(tasks.less(), tasks.ts());
        await tasks.html();
    };

    return parallel(
        tasks.favicons(),
        htmlAll()
    );
});

async function getFaviconDataUrl() {
    const faviconSvg = await jetpack.readAsync(paths.from.favicon);
    // http://codepen.io/jakob-e/pen/doMoML
    // @ts-ignore
    return faviconSvg
        .replace(/"/g, '\'')
        .replace(/%/g, '%25')
        .replace(/#/g, '%23')
        .replace(/{/g, '%7B')
        .replace(/}/g, '%7D')
        .replace(/</g, '%3C')
        .replace(/>/g, '%3E')
        .replace(/\s+/g, ' ');
}

async function getCombinedTemplates() {
    const basePath = `${dirname}/components`;
    const htmlPaths = await jetpack.findAsync(basePath, { matching: '*.html' });
    const htmlPromises = htmlPaths.map(async htmlPath => {
        const template = await jetpack.readAsync(htmlPath);
        const minified = htmlMinifier.minify(template, { collapseWhitespace: true });
        return `<script type="text/x-template" id="${path.basename(htmlPath, '.html')}">${minified}</script>`;
    });
    return (await Promise.all(htmlPromises)).join('\r\n');
}

run();