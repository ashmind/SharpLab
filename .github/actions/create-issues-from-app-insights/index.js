import exec from '@actions/exec';

try {
    await exec.exec('./node_modules/.bin/ts-node-esm index.ts');
}
catch (error) {
    console.error(error);
    process.exit(1);
}