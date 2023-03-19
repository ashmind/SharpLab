import jetpack from 'fs-jetpack';
import { task } from 'oldowan';
import { exec2 } from './shared';

const cm6PreviewRoot = `${__dirname}/../node_modules/mirrorsharp-codemirror-6-preview`;

const rewriteCM6PreviewModulesToExact = task('install:cm6-preview-modules:rewrite-exact', async () => {
    // Switching all dependencies to exact versions, to avoid CodeMirror version duplication
    const packageJsonPath = `${cm6PreviewRoot}/package.json`;
    const packageJson = await jetpack.readAsync(packageJsonPath, 'json') as {
        dependencies: Record<string, string>;
    };
    for (const dependency in packageJson.dependencies) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        packageJson.dependencies[dependency] = packageJson.dependencies[dependency]!.replace(/^[^~]/, '');
    }
    await jetpack.writeAsync(packageJsonPath, packageJson);
});

export const installCM6PreviewModules = task('install:cm6-preview-modules', async () => {
    await rewriteCM6PreviewModulesToExact();
    await exec2('npm', ['install'], { cwd: cm6PreviewRoot });
}, {
    watch: [cm6PreviewRoot]
});