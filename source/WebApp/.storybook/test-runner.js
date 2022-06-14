const { toMatchImageSnapshot } = require('jest-image-snapshot');

module.exports = {
    setup() {
        expect.extend({ toMatchImageSnapshot });
    },

    /**
     * @param {import('playwright').Page} page
     * @param {*} context
     */
    async postRender(page, context) {
        const image = await page.screenshot({ animations: 'disabled' });

        const storyPathParts = context.title.split('/');
        const storyFileName = storyPathParts.pop();
        const storyDir = `${__dirname}/../app/${storyPathParts.join('/')}`;

        expect(image).toMatchImageSnapshot({
            customSnapshotsDir: `${storyDir}/__snapshots__/${storyFileName}`,
            customSnapshotIdentifier: context.name
        });
    },
};