import path from 'path';
import execa from 'execa';

export const inputRoot = path.resolve(__dirname, '..');
export const inputAppRoot = path.resolve(inputRoot, 'app');

export const outputSharedRoot = `${inputRoot}/public`;
const outputVersion = process.env.NODE_ENV === 'production'
    ? (process.env.SHARPLAB_WEBAPP_BUILD_VERSION ?? (() => { throw 'SHARPLAB_WEBAPP_BUILD_VERSION was not provided.'; })())
    : Date.now();
export const outputVersionRoot = `${outputSharedRoot}/${outputVersion}`;

// TODO: expose in oldowan
export const exec2 = (command: string, args: ReadonlyArray<string>, options?: {
    env?: NodeJS.ProcessEnv;
}) => execa(command, args, {
    preferLocal: true,
    stdout: process.stdout,
    stderr: process.stderr,
    env: options?.env
});