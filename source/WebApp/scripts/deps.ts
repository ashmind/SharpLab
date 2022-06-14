import execa from 'execa';
import { exec, task } from 'oldowan';

const unused = task('deps:unused', () => exec('depcheck'));

const duplicates = task('deps:duplicates', async () => {
    const output = (await execa('npm', ['find-dupes'])).stdout;
    // https://github.com/npm/cli/issues/2687
    const potentialDuplicatesOutput = output
        .replace(/added \d+ packages(, and changed \d+ packages)? in \d+(s|m)/, '')
        .replace(/\d+ packages are looking for funding/, '')
        .replace(/run `npm fund` for details/, '')
        .trim();
    if (potentialDuplicatesOutput.length > 0)
        throw new Error(`npm find-duplicates has discovered duplicates:\n${potentialDuplicatesOutput}`);
});

export const deps = task('deps', () => Promise.all([
    unused(),
    duplicates()
]));