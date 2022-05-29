import jetpack from 'fs-jetpack';
import { task } from 'oldowan';
import { iconSizes, iconSvgSourcePath } from './icons';
import { inputRoot, outputVersionRoot } from './shared';

const getFavicons = async () => {
    // http://codepen.io/jakob-e/pen/doMoML
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const faviconSvgUrl = (await jetpack.readAsync(iconSvgSourcePath))!
        .replace(/"/g, '\'')
        .replace(/%/g, '%25')
        .replace(/#/g, '%23')
        .replace(/{/g, '%7B')
        .replace(/}/g, '%7D')
        .replace(/</g, '%3C')
        .replace(/>/g, '%3E')
        .replace(/\s+/g, ' ');

    return `
    <link rel="icon" type="image/svg+xml" href="data:image/svg+xml,${faviconSvgUrl}" data-react-replace>
    ${iconSizes.map(size => `<link rel="icon" type="image/png" href="icon-${size}.png" sizes="${size}x${size}" data-react-replace>`).join(`
    `)}`;
};

const htmlSourcePath = `${inputRoot}/index.html`;
export const htmlOutputPath = `${outputVersionRoot}/index.html`;
export const html = task('html', async () => {
    const htmlMinifier = (await import('html-minifier')).default;

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    let html = (await jetpack.readAsync(htmlSourcePath))!;
    html = html
        .replace('{build:js}', 'app.min.js')
        .replace('{build:css}', 'app.min.css')
        .replace('{build:favicons}', await getFavicons());
    html = htmlMinifier.minify(html, { collapseWhitespace: true });
    await jetpack.writeAsync(htmlOutputPath, html);
}, {
    watch: [
        htmlSourcePath,
        iconSvgSourcePath
    ]
});