import execa from 'execa';
import fetch from 'node-fetch';
import { shouldSkipRender } from '../should-skip';
import { setContainerIdFromSetup } from './container-id';

// TODO: Investigate concurrency issues here.
// For now test workers are limited to 1 in package.json,
// so the setup state "lock" is not really applied.

type RenderSetupState = 'none' | 'pending' | 'ready';
function setSetupState(state: RenderSetupState) {
    process.env.TEST_DOCKER_SETUP_STATE = state;
}
function getSetupState() {
    return (process.env.TEST_DOCKER_SETUP_STATE as RenderSetupState|undefined) ?? 'none';
}
function setPort(port: string) {
    process.env.TEST_DOCKER_PORT = port;
}
function getPort() {
    return process.env.TEST_DOCKER_PORT;
}

async function waitFor(ready: () => Promise<boolean>|boolean, error: () => Error) {
    let remainingRetryCount = 50;
    while (!(await Promise.resolve(ready()))) {
        if (remainingRetryCount === 0)
            throw error();
        await new Promise(resolve => setTimeout(resolve, 100));
        remainingRetryCount -= 1;
    }
}

export default async (): Promise<{ port: string }> => {
    if (shouldSkipRender)
        throw new Error('Setup should not be called if we are skipping render.');

    if (getSetupState() === 'ready')
        return { port: getPort()! };

    if (getSetupState() === 'pending') {
        await waitFor(
            () => getSetupState() === 'ready',
            () => new Error(`Pending setup has not completed within the wait period.`)
        );
        return { port: getPort()! };
    }

    setSetupState('pending');
    const chromeContainerId = (await execa('docker', [
        'container',
        'run',
        '-d',
        '-p',
        '9222',
        '--rm',
        '--security-opt',
        `seccomp=${__dirname}/chrome.seccomp.json`,
        'gcr.io/zenika-hub/alpine-chrome:89',
        '--remote-debugging-address=0.0.0.0',
        `--remote-debugging-port=9222`,
        'about:blank'
    ])).stdout;
    setContainerIdFromSetup(chromeContainerId);
    const port = (await execa('docker', [
        'port',
        chromeContainerId,
        '9222'
    ])).stdout.match(/:(\d+)$/)![1];
    console.log(`Started Chrome container ${chromeContainerId} on port ${port}. Open http://localhost:${port} to debug.`);
    setPort(port);

    await waitFor(async () => {
        try {
            await fetch(`http://localhost:${port}`);
            return true;
        }
        catch {
            return false;
        }
    }, () => new Error(`Chrome container has not opened port ${port} within the wait period.`));

    setSetupState('ready');
    return { port };
};