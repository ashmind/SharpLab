import '../../env';
import fs from 'fs/promises';
import path from 'path';
import chalk from 'chalk';
import execa from 'execa';
import { nodeSafeTopLevelAwait } from '../../shared/nodeSafeTopLevelAwait';
import type { Branch } from '../../shared/types';
import { buildRootPath, rootPath } from '../../shared/paths';
import { getBranchesJson } from '../../shared/branchesJson';
import { getCleanupAction } from './cleanup/getCleanupAction';

const roslynSourcePath = path.join(buildRootPath, 'sources/dotnet.git');

const isCommitMergedToMain = async (commitHash: string) => {
    const { stdout } = await execa.command(`git --no-pager branch --contains ${commitHash} --format=%(refname:short)`, {
        cwd: roslynSourcePath
    });

    return /^main$/m.test(stdout);
};

const run = async () => {
    console.log('Environment:');
    console.log(`  Script Root:   ${__dirname}`);
    console.log(`  Root:          ${rootPath}`);
    console.log(`  Roslyn Source: ${roslynSourcePath}`);
    console.log('');

    const config = JSON.parse(await fs.readFile(`${rootPath}/.roslyn-branches.json`, { encoding: 'utf-8' })) as {
        include: string;
    };

    console.log(chalk.white('Getting git branches...'));
    console.log(`  git --no-pager branch --format=%(refname:short) (at ${roslynSourcePath})`);
    const { stdout: branchesString } = await execa.command('git --no-pager branch --format=%(refname:short)', {
        cwd: roslynSourcePath
    });
    const branches = branchesString
        .split(/[\r\n]+/g)
        .filter(b => new RegExp(config.include).test(b));
    console.log('');

    console.log(chalk.white('Getting branches.json...'));
    console.log('  GET https://slbs.azureedge.net/public/branches.json');
    const branchesJson = await getBranchesJson();
    const branchesNotInGit = branchesJson
        .filter((j): j is (Branch & { kind: 'roslyn' }) => j.kind === 'roslyn')
        .filter(j => !branches.some(b => b === j.name));
    console.log('');

    console.log(chalk.white('Preparing cleanup info...'));
    const cleanup = (await Promise.all(branchesNotInGit.map(async branch => {
        const merged = branch.merged
            ?? await isCommitMergedToMain(branch.commits[0].hash);
        const action = getCleanupAction(branch, merged);

        console.log(`  ${branch.id}`);
        return {
            branch: branch.name,
            action
        };
    }))).filter(a => a.action !== 'wait');

    console.log(chalk.white('Writing matrices...'));
    const buildMatrix = {
        include: branches.map(branch => ({
            branch,
            optional: (branch !== 'main')
        }))
    };
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    await fs.appendFile(process.env.GITHUB_OUTPUT!, `build=${JSON.stringify(buildMatrix)}\n`);

    const cleanupMatrix = { include: cleanup };
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    await fs.appendFile(process.env.GITHUB_OUTPUT!, `cleanup=${JSON.stringify(cleanupMatrix)}\n`);
};

nodeSafeTopLevelAwait(run, e => {
    console.error('::error::' + e);
    process.exit(1);
}, { timeoutMinutes: 5 });