import { dirname } from 'path';
import { fileURLToPath } from 'url';
import exec from '@actions/exec';

try {
    const basePath = dirname(fileURLToPath(import.meta.url));
    await exec.exec(`"${basePath}/node_modules/.bin/ts-node-esm"`, [`${basePath}/index.ts`]);
}
catch (error) {
    console.error(error);
    process.exit(1);
}