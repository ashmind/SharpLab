import jetpack from 'fs-jetpack';
import { task } from 'oldowan';
import { inputRoot, outputVersionRoot } from './shared';

export const iconSizes = [
    16, 32, 64, 72, 96, 120, 128, 144, 152, 180, 192, 196, 256, 384, 512
];

export const iconSvgSourcePath = `${inputRoot}/icon.svg`;
export const icons = task('icons', async () => {
    const sharp = (await import('sharp')).default;

    await jetpack.dirAsync(outputVersionRoot);

    await jetpack.copyAsync(iconSvgSourcePath, `${outputVersionRoot}/icon.svg`, { overwrite: true });
    // Not parallelizing with Promise.all as sharp seems to be prone to timeouts when running in parallel
    for (const size of iconSizes) {
        // https://github.com/lovell/sharp/issues/729
        const density = size > 128 ? Math.round(72 * size / 128) : 72;
        await sharp(iconSvgSourcePath, { density })
            .resize(size, size)
            .png()
            .toFile(`${outputVersionRoot}/icon-${size}.png`);
    }
}, {
    timeout: 60000,
    watch: [iconSvgSourcePath]
});