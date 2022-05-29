import { task } from 'oldowan';
import jetpack from 'fs-jetpack';
import { inputRoot, outputVersionRoot } from './shared';

export const less = task('less', async () => {
    const lessRender = (await import('less')).default;
    const postcss = (await import('postcss')).default;
    // @ts-expect-error (no typings)
    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
    const csso = (await import('postcss-csso')).default;
    const autoprefixer = (await import('autoprefixer')).default;

    const sourcePath = `${inputRoot}/less/app.less`;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const content = (await jetpack.readAsync(sourcePath))!;
    let { css, map } = await lessRender.render(content, {
        filename: sourcePath,
        sourceMap: {
            sourceMapBasepath: inputRoot,
            outputSourceFiles: true
        }
    });
    // @ts-expect-error (TODO: need to sort out 'map' type here)
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
}, { watch: [`${inputRoot}/less/**/*.less`] });