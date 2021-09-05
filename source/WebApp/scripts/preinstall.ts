import { task, build as run } from 'oldowan';
import execa from 'execa';

task('default', async () => {
    const execOptions = {
        cwd: `${__dirname}/../../#external/mirrorsharp/WebAssets`,
        stdout: process.stdout,
        stderr: process.stderr
    };

    await execa('npm', ['install'], execOptions);
    await execa('npm', ['run', 'build'], execOptions);
    await execa('npm', ['install', '--production'], {
        ...execOptions,
        cwd: `${execOptions.cwd}/dist`
    });
});

// eslint-disable-next-line @typescript-eslint/no-floating-promises
run();