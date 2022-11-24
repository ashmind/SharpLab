import '../../env';
import path from 'path';
import fs from 'fs-extra';
import chalk from 'chalk';
import git from 'simple-git';
import { nodeSafeTopLevelAwait } from '../../shared/nodeSafeTopLevelAwait';
import type { Branch } from '../../shared/types';
import { buildRootPath, rootPath } from '../../shared/paths';
import { getBranchesJson } from '../../shared/branchesJson';
import { getCleanupAction } from './cleanup/getCleanupAction';

const ROSLYN_REPO_URL = 'https://github.com/dotnet/roslyn.git';
const roslynSourcePath = path.join(buildRootPath, 'sources/dotnet.git');

const isCommitMergedToMain = async (commitHash: string) => {
    return (await git(roslynSourcePath).branch([`--contains`, commitHash])).all
        .some(b => /^main$/.test(b));
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

    console.log(chalk.white('Cloning Roslyn repository...'));
    await git().clone(ROSLYN_REPO_URL, roslynSourcePath, ['--bare', '--filter=blob:none']);

    console.log(chalk.white('Getting git branches...'));
    const branches = (await git(roslynSourcePath).branchLocal())
        .all.filter(b => new RegExp(config.include).test(b));
    console.log('');

    console.log(chalk.white('Getting branches.json...'));
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

        console.log(`  ${branch.id} => ${action}`);
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
    await fs.appendFile(process.env.GITHUB_OUTPUT!, `update=${JSON.stringify(buildMatrix)}\n`);

    const cleanupMatrix = { include: cleanup };
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    await fs.appendFile(process.env.GITHUB_OUTPUT!, `cleanup=${JSON.stringify(cleanupMatrix)}\n`);
};

nodeSafeTopLevelAwait(run, e => {
    console.error('::error::' + e);
    process.exit(1);
}, { timeoutMinutes: 5 });