import * as path from 'path';
import { task } from 'oldowan';
import jetpack from 'fs-jetpack';
import { inputRoot, outputVersionRoot } from './shared';

export const less = task('less', async () => {
    const lessRender = (await import('less')).default;
    const postcss = (await import('postcss')).default;
    // @ts-expect-error (no typings)
    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
    const csso = (await import('postcss-csso')).default;
    const postcssUrl = (await import('postcss-url')).default;
    const autoprefixer = (await import('autoprefixer')).default;

    const sourcePath = `${inputRoot}/less/app.less`;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const content = (await jetpack.readAsync(sourcePath))!;
    const initial = await lessRender.render(content, {
        filename: sourcePath,
        sourceMap: {
            sourceMapBasepath: inputRoot,
            outputSourceFiles: true
        },
        rewriteUrls: 'local'
    } as Less.Options);
    const fonts = [] as Array<[from: string, to: string]>;
    const { css, map, messages } = await postcss([
        autoprefixer,
        postcssUrl([
            {
                filter: 'node_modules/@fontsource/**/files/*.*',
                url: ({ url, absolutePath }) => {
                    const targetPath = url.replace(
                        /^.*@fontsource\/[^/]+\/files\/(.+)$/,
                        './fonts/$1'
                    );
                    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                    fonts.push([absolutePath!, path.join(outputVersionRoot, targetPath)]);
                    return targetPath;
                }
            }
        ]),
        // no typings for csso
        // eslint-disable-next-line @typescript-eslint/no-unsafe-call
        csso({ restructure: false })
    ]).process(initial.css, {
        from: sourcePath,
        map: {
            inline: false,
            prev: initial.map
        }
    });

    for (const message of messages) {
        const log = message.type === 'warning' ? 'warn' : 'log';
        console[log](message.toString());
    }

    const outputPath = `${outputVersionRoot}/app.min.css`;
    await Promise.all([
        jetpack.writeAsync(outputPath, css),
        jetpack.writeAsync(outputPath + '.map', JSON.stringify(map)),
        ...fonts.map(([from, to]) => jetpack.copyAsync(from, to))
    ]);
}, { watch: [`${inputRoot}/less/**/*.less`] });