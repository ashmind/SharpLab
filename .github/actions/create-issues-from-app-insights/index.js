import { execa } from 'execa';

try {
    await execa('ts-node-esm', ['index.ts'], {
        preferLocal: true,
        stdout: process.stdout,
        stderr: process.stderr
    });
}
catch (error) {
    process.exit(1);
}