import jetpack from 'fs-jetpack';
import { task } from 'oldowan';
import { iconSizes } from './icons';
import { inputRoot, outputVersionRoot } from './shared';

const manifestSourcePath = `${inputRoot}/manifest.json`;
export const manifest = task('manifest', async () => {
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