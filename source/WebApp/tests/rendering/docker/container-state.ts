import path from 'path';
import lockfile from 'proper-lockfile';
import jetpack from 'fs-jetpack';

const CONTAINER_STATE_PATH = path.join(__dirname, '.container');
type ContainerState = { id: string, port: string };

const readState = async () => {
    try {
        return (await jetpack.readAsync(CONTAINER_STATE_PATH, 'json')) as ContainerState;
    }
    catch (e) {
        if ((e as { code?: string }).code === 'ENOENT')
            return null;
        throw e;
    }
};

export const handleContainerState = async <TResult>(
    process: (
        state: ContainerState|null,
        writeState: ((state: ContainerState|null) => Promise<void>)
    ) => Promise<TResult>
) => {
    const unlock = await lockfile.lock(__dirname);
    try {
        const state = await readState();
        return await process(state, async s => {
            if (s === null) {
                await jetpack.removeAsync(CONTAINER_STATE_PATH);
                return;
            }

            await jetpack.writeAsync(CONTAINER_STATE_PATH, s);
        });
    }
    finally {
        await unlock();
    }
};