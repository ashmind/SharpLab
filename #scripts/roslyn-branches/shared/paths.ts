import path from 'path';
import fs from 'fs-extra';

export const rootPath = path.resolve(path.join(__dirname, '..', '..', '..'));
export const buildRootPath = path.join(rootPath, '!roslyn-branches');
fs.ensureDirSync(buildRootPath);