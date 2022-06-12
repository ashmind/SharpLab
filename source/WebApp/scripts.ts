import { promisify } from 'util';
import { task, exec, build as run } from 'oldowan';
import jetpack from 'fs-jetpack';
import waitOn from 'wait-on';
import kill from 'tree-kill';
import { exec2, outputSharedRoot } from './scripts/shared';
import { less } from './scripts/less';
import { ts } from './scripts/ts';
import { deps } from './scripts/deps';
import { icons } from './scripts/icons';
import { manifest } from './scripts/manifest';
import { html, htmlOutputPath } from './scripts/html';

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

task('test-storybook-ci', async () => {
    // await exec2('build-storybook', [], { env: { NODE_ENV: 'test' } });
    console.log('http-server: starting');
    const server = exec2('http-server', ['storybook-static', '--port', '6006', '--silent']);
    console.log('docker: starting');
    const docker = exec2('docker', [
        'container', 'run',
        '-p', '9222:9222',
        '--rm',
        '--security-opt',
        `seccomp=${dirname}/scripts/chrome.seccomp.json`,
        'zenika/alpine-chrome:102',
        '--remote-debugging-address=0.0.0.0',
        `--remote-debugging-port=9222`,
        'about:blank'
    ]);
    try {
        await waitOn({
            resources: [
                'http://localhost:6006',
                'http://localhost:9222'
            ],
            timeout: 60000
        });
        await exec('test-storybook');
    }
    finally {
        if (!server.killed) {
            console.log('http-server: terminating');
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            await promisify(kill)(server.pid!);
        }

        if (!docker.killed) {
            console.log('docker: terminating');
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            await promisify(kill)(docker.pid!);
        }
    }
});

// eslint-disable-next-line @typescript-eslint/no-floating-promises
run();