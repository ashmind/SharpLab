import execa from 'execa';
import { shouldSkipRender } from '../should-skip';
import { getContainerId } from './container-id';

export default async () => {
    if (shouldSkipRender)
        return;

    const chromeContainerId = getContainerId();
    if (!chromeContainerId)
        return;
    await execa('docker', [
        'stop',
        chromeContainerId
    ]);
    console.log('Stopped Chrome container', chromeContainerId);
};