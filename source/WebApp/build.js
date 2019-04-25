console.time('requires');
// Common:
const { task, tasks, run } = require('oldowan');
const path = require('path');
const jetpack = require('fs-jetpack');
const md5File = require('md5-file/promise');
// CSS:
const less = require('less');
const autoprefixer = require('autoprefixer');
const postcss = require('postcss');
const csso = require('postcss-csso');
// JS:
const rollup = require('rollup');
const rollupPluginNodeResolve = require('rollup-plugin-node-resolve');
const rollupPluginCommonJS = require('rollup-plugin-commonjs');
const rollupPluginTerser = require('rollup-plugin-terser').terser;
// Favicons:
const sharp = require('sharp');
// HTML:
const htmlMinifier = require('html-minifier');
console.timeEnd('requires');

const outputRoot = `${__dirname}/wwwroot`;
const production = process.env.NODE_ENV === 'production';

const parallel = (...promises) => Promise.all(promises);

const paths = {
    from: {
        less: `${__dirname}/less/app.less`,
        js: `${__dirname}/js/app.js`,
        favicon: `${__dirname}/favicon.svg`,
        html: `${__dirname}/index.html`
    },
    to: {
        css: `${outputRoot}/app.min.css`,
        js: `${outputRoot}/app.min.js`,
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
            sourceMapBasepath: `${__dirname}`,
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
}, { inputs: `${__dirname}/less/**/*.less` });

task('js', async () => {
    const bundle = await rollup.rollup({
        // https://github.com/rollup/rollup/issues/2473
        treeshake: false,
        input: paths.from.js,
        plugins: [
            rollupPluginCommonJS({
                include: [
                    'node_modules/**',
                    'components/internal/codemirror/**',
                    'js/ui/helpers/**'
                ]
            }),
            {
                name: 'rollup-plugin-adhoc-resolve-vue',
                resolveId: id => (id === 'vue') ? path.resolve(`./node_modules/vue/dist/vue${production?'.min':''}.js`) : null
            },
            rollupPluginNodeResolve({ browser: true }),
            ...(production ? [rollupPluginTerser()] : [])
        ]
    });

    await bundle.write({
        format: 'iife',
        file: paths.to.js,
        sourcemap: true
    });
}, {
    inputs: [
        `${__dirname}/js/**/*.js`,
        `${__dirname}/components/**/*.js`,
        `${__dirname}/package.json`
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
            .toFile(paths.to.favicon.png.replace('{size}', size));
    });

    return parallel(
        jetpack.copyAsync(paths.from.favicon, paths.to.favicon.svg, { overwrite: true }),
        pngGeneration
    );
}, { inputs: paths.from.favicon });

task('html', async () => {
    const roslynVersion = await getRoslynVersion();
    const faviconDataUrl = await getFaviconDataUrl();
    const templates = await getCombinedTemplates();
    const [jsHash, cssHash] = await parallel(
        md5File('wwwroot/app.min.js'),
        md5File('wwwroot/app.min.css')
    );
    let html = await jetpack.readAsync(paths.from.html);
    html = html
        .replace('{build:js}', 'app.min.js?' + jsHash)
        .replace('{build:css}', 'app.min.css?' + cssHash)
        .replace('{build:templates}', templates)
        .replace('{build:favicon-svg}', faviconDataUrl)
        .replace(/\{build:roslyn-version\}/g, roslynVersion);
    html = htmlMinifier.minify(html, { collapseWhitespace: true });
    await jetpack.writeAsync(paths.to.html, html);
}, {
    inputs: [
        `${__dirname}/components/**/*.html`,
        paths.to.css,
        paths.to.js,
        paths.from.html,
        paths.from.favicon
    ]
});

task('default', () => {
    const htmlAll = async () => {
        await parallel(tasks.less(), tasks.js());
        await tasks.html();
    };

    return parallel(
        tasks.favicons(),
        htmlAll()
    );
});

async function getRoslynVersion() {
    const assetsJson = JSON.parse(await jetpack.readAsync('../Server/obj/project.assets.json'));
    for (const key in assetsJson.libraries) {
        const match = key.match(/^Microsoft\.CodeAnalysis\.Common\/(.+)$/);
        if (match)
            return match[1];
    }
    return null;
}

async function getFaviconDataUrl() {
    const faviconSvg = await jetpack.readAsync(paths.from.favicon);
    // http://codepen.io/jakob-e/pen/doMoML
    return faviconSvg
        .replace(/"/g, '\'')
        .replace(/%/g, '%25')
        .replace(/#/g, '%23')
        .replace(/{/g, '%7B')
        .replace(/}/g, '%7D')
        .replace(/</g, '%3C')
        .replace(/>/g, '%3E')
        .replace(/\s+/g,' ');
}

async function getCombinedTemplates() {
    const basePath = `${__dirname}/components`;
    const htmlPaths = await jetpack.findAsync(basePath, { matching: '*.html' });
    const htmlPromises = htmlPaths.map(async htmlPath => {
        const template = await jetpack.readAsync(htmlPath);
        const minified = htmlMinifier.minify(template, { collapseWhitespace: true });
        return `<script type="text/x-template" id="${path.basename(htmlPath, '.html')}">${minified}</script>`;
    });
    return (await Promise.all(htmlPromises)).join('\r\n');
}

run();