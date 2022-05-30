import exec from '@actions/exec';

try {
    await exec.exec('ts-node-esm index.ts');
}
catch (error) {
    process.exit(1);
}