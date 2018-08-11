const jetpack = require('fs-jetpack');

const tasks = {};

function formatDuration([seconds, nanoseconds]) {
    if (seconds === 0) {
        if (nanoseconds > 1000000)
            return Math.round(nanoseconds / 1000000) + 'ms';
        return nanoseconds + 'ns';
    }

    if (seconds < 60)
        return `${seconds}.${Math.round(nanoseconds / 10000000)}s`;

    return `${Math.floor(seconds / 60)}m${seconds % 60}s`;
}

function taskError(e, taskName) {
    if (e.fromTask) {
        console.error(`task ${taskName} failed: task ${e.fromTask} failed`);
    }
    else {
        console.error(`task ${taskName} failed:`);
        console.error(e);
    }
    const error = new Error();
    error.fromTask = taskName;
    throw error;
}

function defineTask(name, body) {
    const run = async (...args) => {
        if (args.length > 0)
            taskError(new Error(`Tasks do not support arguments (provided: ${args})`), name);

        console.log(`task ${name} starting...`);
        const startTime = process.hrtime();
        try {
            await Promise.resolve(body());
        }
        catch(e) {
            taskError(e, name);
        }
        const duration = process.hrtime(startTime);
        console.log(`task ${name} completed [${formatDuration(duration)}]`);
    };
    tasks[name] = run;
    return run;
}

async function build() {
    const taskName = process.argv[2] || 'default'; // node x.js taskName
    const task = tasks[taskName];
    if (!task) {
        console.error(`Unknown task: ${taskName}`);
        console.error(`Registered tasks:\r\n  ${Object.keys(tasks).join('\r\n  ')}`);
        process.exit(1);
    }

    try {
        await task();
    }
    catch (e) {
        if (!e.fromTask)
            console.error(e);
        process.exit(1);
    }

    process.exit(0);
}

module.exports = {
    task: defineTask,
    tasks,
    build,
    jetpack
};