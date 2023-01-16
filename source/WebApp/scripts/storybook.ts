import { promisify } from 'util';
import kill from 'tree-kill';
import jetpack from 'fs-jetpack';
import { task } from 'oldowan';
import waitOn from 'wait-on';
import { exec2, inputAppRoot, inputRoot } from './shared';

const UPDATE_SNAPSHOTS_KEY = 'SHARPLAB_TEST_UPDATE_SNAPSHOTS';

task('storybook:build', async () => {
    // Important not to build it in production mode, as
    // it will prevent dev-only React hacks used by e.g. favicon stories
    await exec2('build-storybook', [], { env: { NODE_ENV: 'testing' } });
});

task('storybook:test:in-container', async () => {
    console.log('http-server: starting');
    const server = exec2('http-server', ['storybook-static', '--port', '6006', '--silent']);
    try {
        await waitOn({
            resources: ['http://localhost:6006'],
            timeout: 120000
        });
        console.log('http-server: ready');

        const updateSnapshots = process.env[UPDATE_SNAPSHOTS_KEY] === 'true';
        console.log(`Starting Storybook tests${updateSnapshots ? ' (with snapshot update)' : ''}...`);
        await exec2('test-storybook', updateSnapshots ? ['--', '--updateSnapshot'] : []);
    }
    finally {
        if (!server.killed) {
            console.log('http-server: terminating');
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            await promisify(kill)(server.pid!);
        }
    }
}, {
    timeout: 20 * 60 * 1000
});

const test = task('storybook:test', async () => {
    console.log('Starting Docker...');
    await exec2('docker', [
        'run',
        '--rm',
        '--ipc=host',
        `--volume=${inputRoot}:/work`,
        '--workdir=/work',
        ...(process.env[UPDATE_SNAPSHOTS_KEY] === 'true' ? ['--env', `${UPDATE_SNAPSHOTS_KEY}=true`] : []),
        'mcr.microsoft.com/playwright:v1.22.2-focal',
        'npm', 'run', 'test-storybook-in-container'
    ]);
}, {
    timeout: 20 * 60 * 1000
});

const clean = task('storybook:test:clean', async () => {
    const snapshotPaths = await jetpack.findAsync(inputAppRoot, {
        matching: '**/__snapshots__/**'
    });
    for (const path of snapshotPaths) {
        await jetpack.removeAsync(path);
    }
});

task('storybook:test:update', async () => {
    await clean();
    process.env[UPDATE_SNAPSHOTS_KEY] = 'true';
    await test();
}, {
    timeout: 20 * 60 * 1000
});