import fs from 'fs';
import path from 'path';
import chalk from 'chalk';
import execa from 'execa';

const root = path.resolve(`${__dirname}/../..`);

console.log('Environment:');
console.log(`  Script Root:  ${__dirname}`);
console.log(`  Root:         ${root}`);
console.log('');

const config = JSON.parse(fs.readFileSync(`${root}/.roslyn-branches.json`, { encoding: 'utf-8' })) as {
    include: string;
};

console.log(chalk.white('Getting branches...'));
console.log('  git ls-remote --heads https://github.com/dotnet/roslyn.git');
const { stdout: branchesString } = execa.commandSync('git ls-remote --heads https://github.com/dotnet/roslyn.git');
const branches = branchesString
    .split(/[\r\n]+/g)
    .map(b => b.replace(/.*refs\/heads\/(\S+).*$/, '$1'))
    .filter(b => new RegExp(config.include).test(b));
console.log('');

console.log(chalk.white('Writing matrix...'));
const matrix = {
    include: branches.map(branch => ({
        branch,
        optional: (branch !== 'master')
    }))
};

console.log(`::set-output name=matrix::${JSON.stringify(matrix)}`);