import execa from 'execa';
import fetch from 'node-fetch';
import { shouldSkipRender } from '../should-skip';
import { waitFor } from '../wait-for';
import { readContainerState, handleContainerState } from './container-state';

export default async (): Promise<{ port: string }> => {
    // console.log('lazySetup()');
    if (shouldSkipRender)
        throw new Error('Setup should not be called if we are skipping render.');

    let state = await readContainerState();
    if (state)
        return { port: state.port };

    state = await handleContainerState(async (currentState, writeState) => {
        if (currentState)
            return currentState;

        const chromeContainerId = (await execa('docker', [
            'container',
            'run',
            '-d',
            '-p', '9222',
            '--rm',
            '--security-opt',
            `seccomp=${__dirname}/chrome.seccomp.json`,
            'gcr.io/zenika-hub/alpine-chrome:89',
            '--remote-debugging-address=0.0.0.0',
            `--remote-debugging-port=9222`,
            'about:blank'
        ])).stdout;
        const port = (await execa('docker', [
            'port',
            chromeContainerId,
            '9222'
        ])).stdout.match(/:(\d+)$/)![1];
        console.log(`Started Chrome container ${chromeContainerId} on port ${port}. Open http://localhost:${port} to debug.`);

        await waitFor(async () => {
            try {
                await fetch(`http://localhost:${port}`);
                return true;
            }
            catch {
                return false;
            }
        }, () => new Error(`Chrome container has not opened port ${port} within the wait period.`));

        const newState = { id: chromeContainerId, port };
        await writeState(newState);
        return newState;
    });

    return { port: state.port };
};