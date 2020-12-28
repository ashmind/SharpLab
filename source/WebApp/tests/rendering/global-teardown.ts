import execa from 'execa';
import { global } from './global-setup';

export default async () => {
    const { chromeContainerId } = global;
    if (!chromeContainerId)
        return;
    await execa('docker', [
        'stop',
        chromeContainerId
    ]);
    console.log('Stopped Chrome container', chromeContainerId);
}