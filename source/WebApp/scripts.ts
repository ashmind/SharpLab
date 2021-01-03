import path from 'path';
import { task, exec, build as run } from 'oldowan';
import execa from 'execa';
import jetpack from 'fs-jetpack';
import lessRender from 'less';
import postcss from 'postcss';
import autoprefixer from 'autoprefixer';
// eslint-disable-next-line @typescript-eslint/ban-ts-ignore
// @ts-ignore (no typings)
import csso from 'postcss-csso';
import sharp from 'sharp';
import htmlMinifier from 'html-minifier';
import AdmZip from 'adm-zip';

const dirname = __dirname;

const outputSharedRoot = `${dirname}/public`;
const outputVersionRoot = `${outputSharedRoot}/${process.env.GITHUB_RUN_NUMBER ?? Date.now()}`;

// TODO: expose in oldowan
const exec2 = (command: string, args: ReadonlyArray<string>) => execa(command, args, {
    preferLocal: true,
    stdout: process.stdout,
    stderr: process.stderr
});

const iconSizes = [
    16, 32, 64, 72, 96, 120, 128, 144, 152, 180, 192, 196, 256, 384, 512
];

const less = task('less', async () => {
    const sourcePath = `${dirname}/less/app.less`;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const content = (await jetpack.readAsync(sourcePath))!;
    let { css, map } = await lessRender.render(content, {
        filename: sourcePath,
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
        from: sourcePath,
        map: {
            inline: false,
            prev: map
        }
    }));

    const outputPath = `${outputVersionRoot}/app.min.css`;
    await Promise.all([
        jetpack.writeAsync(outputPath, css),
        jetpack.writeAsync(outputPath + '.map', map)
    ]);
}, { watch: [`${dirname}/less/**/*.less`] });

const tsLint = task('ts-lint', () => exec('eslint . --max-warnings 0 --ext .js,.ts'));
const jsOutputPath = `${outputVersionRoot}/app.min.js`;
const tsMain = task('ts-main', () => exec2('rollup', ['-c', '-o', jsOutputPath]), {
    watch: () => exec2('rollup', ['-c', '-w', '-o', jsOutputPath])
});

const ts = task('ts', async () => {
    await tsLint();
    await tsMain();
});

const iconSvgSourcePath = `${dirname}/icon.svg`;
const icons = task('icons', async () => {
    await jetpack.dirAsync(outputVersionRoot);
    const pngGeneration = iconSizes.map(async size => {
        // https://github.com/lovell/sharp/issues/729
        const density = size > 128 ? Math.round(72 * size / 128) : 72;
        await sharp(iconSvgSourcePath, { density })
            .resize(size, size)
            .png()
            .toFile(`${outputVersionRoot}/icon-${size}.png`);
    });

    return Promise.all([
        jetpack.copyAsync(iconSvgSourcePath, `${outputVersionRoot}/icon.svg`, { overwrite: true }),
        ...pngGeneration
    ]);
}, {
    timeout: 10000,
    watch: [iconSvgSourcePath]
});

const manifestSourcePath = `${dirname}/manifest.json`;
const manifest = task('manifest', async () => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const content = JSON.parse((await jetpack.readAsync(manifestSourcePath))!) as {
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

    await jetpack.writeAsync(`${outputVersionRoot}/manifest.json`, JSON.stringify(content));
}, { watch: [manifestSourcePath] });

const htmlSourcePath = `${dirname}/index.html`;
const htmlOutputPath = `${outputVersionRoot}/index.html`;
const html = task('html', async () => {
    const iconDataUrl = await getIconDataUrl();
    const templates = await getCombinedTemplates();

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    let html = (await jetpack.readAsync(htmlSourcePath))!;
    html = html
        .replace('{build:js}', 'app.min.js')
        .replace('{build:css}', 'app.min.css')
        .replace('{build:templates}', templates)
        .replace('{build:favicon-svg}', iconDataUrl);
    html = htmlMinifier.minify(html, { collapseWhitespace: true });
    await jetpack.writeAsync(htmlOutputPath, html);
}, {
    watch: [
        `${dirname}/components/**/*.html`,
        htmlSourcePath,
        iconSvgSourcePath
    ]
});

const latest = task('latest', () => jetpack.writeAsync(
    `${outputSharedRoot}/latest`, htmlOutputPath.replace(outputSharedRoot, '').replace(/^[\\/]/, '')
));

const build = task('build', async () => {
    await jetpack.removeAsync(outputSharedRoot);
    await Promise.all([
        less(),
        ts(),
        icons(),
        manifest(),
        html(),
        latest()
    ]);
});

task('start', () => build(), {
    watch: async () => exec2('http-server', [outputSharedRoot, '-p', '54200', '--cors'])
});

// Assumes we already ran the build
const zip = task('zip', () => {
    const zip = new AdmZip();
    zip.addLocalFolder(outputSharedRoot);
    zip.writeZip(`${dirname}/WebApp.zip`);
});

task('build-ci', async () => {
    if (process.env.NODE_ENV !== 'ci')
        throw new Error('Command build-ci should only be run under NODE_ENV=ci.');
    await build();
    await zip();
});

async function getIconDataUrl() {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const faviconSvg = (await jetpack.readAsync(iconSvgSourcePath))!;
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
run();