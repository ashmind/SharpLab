import execa from 'execa';
import fetch from 'node-fetch';
import { setPortFromGlobalSetup } from './port';

const typedGlobal = global as unknown as {
    chromeContainerId: string;
};

export default async () => {
    const chromeContainerId = (await execa('docker', [
        'container',
        'run',
        '-d',
        '-p',
        '9222',
        '--rm',
        '--security-opt',
        `seccomp=${__dirname}/chrome.seccomp.json`,
        'gcr.io/zenika-hub/alpine-chrome',
        '--remote-debugging-address=0.0.0.0',
        `--remote-debugging-port=9222`,
        'about:blank'
    ])).stdout;
    typedGlobal.chromeContainerId = chromeContainerId;
    const port = (await execa('docker', [
        'port',
        chromeContainerId,
        '9222'
    ])).stdout.match(/:(\d+)$/)![1];
    console.log('Started Chrome container', chromeContainerId, 'on port', port);
    setPortFromGlobalSetup(port);

    let remainingRetryCount = 50;
    let ready = false;
    do {
        if (remainingRetryCount === 0)
            throw new Error(`Chrome container has not opened port ${port} within the wait period.`);
        await new Promise(resolve => setTimeout(resolve, 100));
        try {
            await fetch(`http://localhost:${port}`);
            ready = true;
        }
        // eslint-disable-next-line no-empty
        catch {
        }
        remainingRetryCount -= 1;
    } while (!ready);
};

export { typedGlobal as global };