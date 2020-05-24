// Common:
import path from 'path';
import { task, exec, build } from 'oldowan';
import jetpack from 'fs-jetpack';
import md5File from 'md5-file/promise';
// CSS:
import lessRender from 'less';
import postcss from 'postcss';
import autoprefixer from 'autoprefixer';
// eslint-disable-next-line @typescript-eslint/ban-ts-ignore
// @ts-ignore (no typings)
import csso from 'postcss-csso';
// Favicons:
import sharp from 'sharp';
// HTML:
import htmlMinifier from 'html-minifier';

const dirname = __dirname;

const outputRoot = `${dirname}/wwwroot`;

const parallel = (...promises: ReadonlyArray<Promise<unknown>>) => Promise.all(promises);

const paths = {
    from: {
        less: `${dirname}/less/app.less`,
        icon: `${dirname}/icon.svg`,
        html: `${dirname}/index.html`,
        manifest: `${__dirname}/manifest.json`
    },
    to: {
        css: `${outputRoot}/app.min.css`,
        icon: {
            svg: `${outputRoot}/icon.svg`,
            png: `${outputRoot}/icon-{size}.png`
        },
        html: `${outputRoot}/index.html`,
        manifest: `${outputRoot}/manifest.json`
    }
};

const iconSizes = [
    16, 32, 64, 72, 96, 120, 128, 144, 152, 180, 192, 196, 256, 384, 512
];

const less = task('less', async () => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const content = (await jetpack.readAsync(paths.from.less))!;
    let { css, map } = await lessRender.render(content, {
        filename: paths.from.less,
        sourceMap: {
            sourceMapBasepath: `${dirname}`,
            outputSourceFiles: true
        }
    });
    // eslint-disable-next-line @typescript-eslint/ban-ts-ignore
    // @ts-ignore (need to sort out 'map' type here)
    ({ css, map } = await postcss([
        autoprefixer,
        // no typings for csso
        // eslint-disable-next-line @typescript-eslint/no-unsafe-call
        csso({ restructure: false })
    ]).process(css, {
        from: paths.from.less,
        map: {
            inline: false,
            prev: map
        }
    }));

    await parallel(
        jetpack.writeAsync(paths.to.css, css),
        jetpack.writeAsync(paths.to.css + '.map', map)
    );
}, { watch: [`${dirname}/less/**/*.less`] });

const tsLint = task('ts-lint', () => exec('eslint . --max-warnings 0 --ext .js,.ts'));
const tsMain = task('ts-main', () => exec('rollup -c'), { watch: () => exec('rollup -c -w') })

const ts = task('ts', async () => {
    await tsLint();
    await tsMain();
});

const icons = task('icons', async () => {
    await jetpack.dirAsync(outputRoot);
    const pngGeneration = iconSizes.map(size => {
        // https://github.com/lovell/sharp/issues/729
        const density = size > 128 ? Math.round(72 * size / 128) : 72;
        return sharp(paths.from.icon, { density })
            .resize(size, size)
            .png()
            .toFile(paths.to.icon.png.replace('{size}', size.toString()));
    });

    return parallel(
        jetpack.copyAsync(paths.from.icon, paths.to.icon.svg, { overwrite: true }),
        ...pngGeneration
    ) as unknown as Promise<void>;
}, { watch: [paths.from.icon] });

const manifest = task('manifest', async () => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const content = JSON.parse((await jetpack.readAsync(paths.from.manifest))!) as {
        icons: ReadonlyArray<{ src: string }>;
    };

    content.icons = content.icons.flatMap(icon => {
        if (!icon.src.includes('{build:each-size}'))
            return [icon];

        const template = JSON.stringify(icon); // simpler than Object.entries
        return iconSizes.map(size => JSON.parse(
            template.replace(/\{(?:build:each-)?size\}/g, size.toString())
        ) as typeof icon);
    });

    await jetpack.writeAsync(paths.to.manifest, JSON.stringify(content));
}, { watch: [paths.from.manifest] });

const html = task('html', async () => {
    const iconDataUrl = await getIconDataUrl();
    const templates = await getCombinedTemplates();
    const [jsHash, cssHash] = await parallel(
        md5File('wwwroot/app.min.js'),
        md5File('wwwroot/app.min.css')
    );
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    let html = (await jetpack.readAsync(paths.from.html))!;
    html = html
        .replace('{build:js}', 'app.min.js?' + jsHash)
        .replace('{build:css}', 'app.min.css?' + cssHash)
        .replace('{build:templates}', templates)
        .replace('{build:favicon-svg}', iconDataUrl);
    html = htmlMinifier.minify(html, { collapseWhitespace: true });
    await jetpack.writeAsync(paths.to.html, html);
}, {
    watch: [
        `${dirname}/components/**/*.html`,
        paths.to.css,
        `${outputRoot}/app.min.js`,
        paths.from.html,
        paths.from.icon
    ]
});

task('default', () => {
    const htmlAll = async () => {
        await parallel(less(), ts());
        await html();
    };

    return parallel(
        icons(),
        manifest(),
        htmlAll()
    ) as unknown as Promise<void>;
});

async function getIconDataUrl() {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const faviconSvg = (await jetpack.readAsync(paths.from.icon))!;
    // http://codepen.io/jakob-e/pen/doMoML
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
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const template = (await jetpack.readAsync(htmlPath))!;
        const minified = htmlMinifier.minify(template, { collapseWhitespace: true });
        return `<script type="text/x-template" id="${path.basename(htmlPath, '.html')}">${minified}</script>`;
    });
    return (await Promise.all(htmlPromises)).join('\r\n');
}

// eslint-disable-next-line @typescript-eslint/no-floating-promises
build();