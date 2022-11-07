import '../../env';
import fs from 'fs/promises';
import path from 'path';
import chalk from 'chalk';
import execa from 'execa';
import safeFetch from '../../shared/safeFetch';
import { nodeSafeTopLevelAwait } from '../../shared/nodeSafeTopLevelAwait';
import type { Branch, CleanupBranch } from '../../shared/types';

const run = async () => {
    const root = path.resolve(`${__dirname}/../..`);

    console.log('Environment:');
    console.log(`  Script Root:  ${__dirname}`);
    console.log(`  Root:         ${root}`);
    console.log('');

    const config = JSON.parse(await fs.readFile(`${root}/.roslyn-branches.json`, { encoding: 'utf-8' })) as {
        include: string;
    };

    console.log(chalk.white('Getting git branches...'));
    console.log('  git ls-remote --heads https://github.com/dotnet/roslyn.git');
    const { stdout: branchesString } = await execa.command('git ls-remote --heads https://github.com/dotnet/roslyn.git');
    const branches = branchesString
        .split(/[\r\n]+/g)
        .map(b => b.replace(/.*refs\/heads\/(\S+).*$/, '$1'))
        .filter(b => new RegExp(config.include).test(b));
    console.log('');

    console.log(chalk.white('Getting branches.json...'));
    console.log('  GET https://slbs.azureedge.net/public/branches.json');
    const branchesJson = await (await safeFetch('https://slbs.azureedge.net/public/branches.json')).json() as Array<Branch>;
    const branchesNotInGit = branchesJson
        .filter((j): j is (Branch & { kind: 'roslyn' }) => j.kind === 'roslyn')
        .filter(j => !branches.some(b => b === j.name));
    console.log('');
    
    console.log(chalk.white('Writing matrices...'));
    const buildMatrix = {
        include: branches.map(branch => ({
            branch,
            optional: (branch !== 'main')
        }))
    };
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    await fs.appendFile(process.env.GITHUB_OUTPUT!, `build=${JSON.stringify(buildMatrix)}\n`);

    const cleanupMatrix = {
        include: branchesNotInGit.map(branch => ({
            branch: branch.name,
            commit: branch.commits[0].hash,
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            app: branch.url.match(/([^./]+)\.azurewebsites\.net/)![1]
        } as CleanupBranch))
    };
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    await fs.appendFile(process.env.GITHUB_OUTPUT!, `cleanup=${JSON.stringify(cleanupMatrix)}\n`);
};

nodeSafeTopLevelAwait(run, e => {
    console.error('::error::' + e);
    process.exit(1);
}, { timeoutMinutes: 5 });