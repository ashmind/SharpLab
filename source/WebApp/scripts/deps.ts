import execa from 'execa';
import { task } from 'oldowan';

const depsCheckDuplicates = task('deps:check-duplicates', async () => {
    const output = (await execa('npm', ['find-dupes'])).stdout;
    // https://github.com/npm/cli/issues/2687
    const potentialDuplicatesOutput = output
        .replace(/added \d+ packages in \d+(s|m)/, '')
        .replace(/\d+ packages are looking for funding/, '')
        .replace(/run `npm fund` for details/, '')
        .trim();
    if (potentialDuplicatesOutput.length > 0)
        throw new Error(`npm find-duplicates has discovered duplicates:\n${potentialDuplicatesOutput}`);
});

export const deps = task('deps', () => Promise.all([
    depsCheckDuplicates()
]));