import execa from 'execa';
import { shouldSkipRender } from '../should-skip';
import { handleContainerState } from './container-state';

export default async () => {
    if (shouldSkipRender)
        return;

    await handleContainerState(async (state, writeState) => {
        if (!state)
            return;

        await execa('docker', [
            'stop',
            state.id
        ]);
        console.log('Stopped Chrome container', state.id);
        await writeState(null);
    });
};