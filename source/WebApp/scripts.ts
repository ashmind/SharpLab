import { task, build as run } from 'oldowan';
import jetpack from 'fs-jetpack';
import { exec2, outputSharedRoot } from './scripts/shared';
import { less } from './scripts/less';
import { ts } from './scripts/ts';
import { deps } from './scripts/deps';
import { icons } from './scripts/icons';
import { manifest } from './scripts/manifest';
import { html, htmlOutputPath } from './scripts/html';
import './scripts/storybook';

const dirname = __dirname;

const latest = task('latest', () => jetpack.writeAsync(
    `${outputSharedRoot}/latest`, htmlOutputPath.replace(outputSharedRoot, '').replace(/^[\\/]/, '')
));

const build = task('build', async () => {
    await jetpack.removeAsync(outputSharedRoot);
    await Promise.all([
        deps(),
        less(),
        ts(),
        icons(),
        manifest(),
        html(),
        latest()
    ]);
});

task('start', () => build(), {
    watch: () => exec2('http-server', [outputSharedRoot, '-p', '44200', '--cors'])
});

// Assumes we already ran the build
const zip = task('zip', async () => {
    const AdmZip = (await import('adm-zip')).default;

    const zip = new AdmZip();
    zip.addLocalFolder(outputSharedRoot);
    zip.writeZip(`${dirname}/WebApp.zip`);
});

task('build-ci', async () => {
    if (process.env.NODE_ENV !== 'production')
        throw new Error('Command build-ci should only be run under NODE_ENV=production.');
    await build();
    await zip();
});

// eslint-disable-next-line @typescript-eslint/no-floating-promises
run();