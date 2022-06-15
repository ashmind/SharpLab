const { toMatchImageSnapshot } = require('jest-image-snapshot');

/** @type {import('@storybook/test-runner').TestRunnerConfig} */
const config = {
    setup() {
        expect.extend({ toMatchImageSnapshot });
    },

    async postRender(page, { title, name }) {
        // https://github.com/storybookjs/test-runner/issues/97#issuecomment-1134419035
        const viewportParameters = await page.evaluate("window.STORY_VIEWPORT_PARAMETERS");
        if (viewportParameters) {
            const viewport = viewportParameters.viewports[viewportParameters.defaultViewport];
            await page.setViewportSize({
                width: parseInt(viewport.styles.width, 10),
                height: parseInt(viewport.styles.height, 10)
            });
        }

        const image = await page.screenshot({ animations: 'disabled' });

        const storyPathParts = title.split('/');
        const storyFileName = storyPathParts.pop();
        const storyDir = `${__dirname}/../app/${storyPathParts.join('/')}`;

        expect(image).toMatchImageSnapshot({
            customSnapshotsDir: `${storyDir}/__snapshots__/${storyFileName}`,
            customSnapshotIdentifier: name
        });
    }
};

module.exports = config;