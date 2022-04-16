import path from 'path';
import fs from 'fs';
import puppeteer, { Page } from 'puppeteer';
import lazyRenderSetup from './docker/lazy-setup';

function getCachePath(url: string) {
    return path.join(__dirname, '__request_cache__', url.replace(/[^a-z._=+-]/ig, '_') + '.json');
}

function isDataRequest(url: string) {
    return url.startsWith('data:');
}

async function setupRequestInterception(page: Page) {
    await page.setRequestInterception(true);

    const cachedRequests = new Set<puppeteer.HTTPRequest>();
    const startedRequests = new Set<puppeteer.HTTPRequest>();
    const finishedRequests = new Set<puppeteer.HTTPRequest>();
    page.on('request', request => {
        // console.log(`request ${request.method()} ${request.url()}`);
        const method = request.method();
        const url = request.url();
        if (method !== 'GET') {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            request.abort();
            return;
        }

        startedRequests.add(request);
        if (isDataRequest(url)) {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            request.continue();
            return;
        }

        const cachePath = getCachePath(url);
        // eslint-disable-next-line no-sync
        if (!fs.existsSync(cachePath)) {
            // eslint-disable-next-line no-console
            console.log(`${method} ${url} - not cached yet`);
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            request.continue();
            return;
        }

        cachedRequests.add(request);
        // eslint-disable-next-line no-sync
        const json = JSON.parse(fs.readFileSync(cachePath, { encoding: 'utf-8' })) as {
            readonly headers: Record<string, string>;
            readonly body: string;
        };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        request.respond({
            headers: json.headers,
            body: Buffer.from(json.body, 'base64')
        });
    });

    page.on('requestfinished', request => {
        // console.log(`requestfinished ${request.method()} ${request.url()}`);
        const method = request.method();
        const url = request.url();

        finishedRequests.add(request);

        if (cachedRequests.has(request) || method !== 'GET' || isDataRequest(url))
            return;

        const response = request.response();
        if (!response || response.status() !== 200)
            return;

        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        (async () => {
            const data = {
                headers: response.headers(),
                body: (await response.buffer()).toString('base64')
            };

            // eslint-disable-next-line no-sync
            fs.writeFileSync(getCachePath(request.url()), JSON.stringify(data, null, 4));
            // eslint-disable-next-line no-console
            console.log(`${method} ${url} - added to cache`);
        })();
    });

    return {
        async waitForUnfinishedRequests() {
            const getUnfinishedRequests = () => [...startedRequests].filter(r => !finishedRequests.has(r));
            let unfinishedRequests = getUnfinishedRequests();
            let remainingRetryCount = 100;
            while (unfinishedRequests.length > 0) {
                //console.log(`waitForUnfinishedRequests(): ${unfinishedRequests.size} unfinished request(s). List: \n${[...unfinishedRequests].map(r => `- ${r.method()} ${r.url()}`).join('\n')}`);
                await new Promise(resolve => setTimeout(resolve, 100));

                remainingRetryCount -= 1;
                if (remainingRetryCount === 0)
                    throw new Error(`Timed out while waiting for unfinished requests: \n${[...unfinishedRequests].map(r => `- ${r.method()} ${r.url()}`).join('\n')}`);
                unfinishedRequests = getUnfinishedRequests();
            }
        }
    };
}

export { shouldSkipRender } from './should-skip';
export default async function render({
    html,
    bodyClass = '',
    styles = [],
    width = 800,
    height = 600,

    debug = false
}: {
    html: string;
    bodyClass?: string;
    styles?: ReadonlyArray<{ path: string }>;
    width?: number;
    height?: number;
    debug?: boolean;
}) {
    // console.log(`render() starting`);
    const content = `<!DOCTYPE html><html><head></head><body class="${bodyClass}">${html}</body></html>`;

    // console.log(`render(): await lazyRenderSetup()`);
    const { port } = await lazyRenderSetup();

    // console.log(`render(): await puppeteer.connect()`);
    const browser = await puppeteer.connect({ browserURL: `http://localhost:${port}` });
    // console.log(`render(): await browser.newPage()`);
    const page = await browser.newPage();

    // console.log(`render(): await setupRequestInterception()`);
    const { waitForUnfinishedRequests } = await setupRequestInterception(page);

    // console.log(`render(): await page.setViewport()`);
    await page.setViewport({ width, height });
    // console.log(`render(): await page.setContent()`);
    await page.setContent(content);
    for (const style of styles) {
        // console.log(`render(): await page.addStyleTag()`);
        await page.addStyleTag(style);
    }
    // console.log(`render(): await waitForUnfinishedRequests()`);
    await waitForUnfinishedRequests();
    // console.log(`render(): await page.evaluate()`);
    await page.evaluate(() => document.fonts.ready);

    // console.log(`render(): await page.screenshot()`);
    const screenshot = await page.screenshot();
    if (debug)
        debugger; // eslint-disable-line no-debugger

    // console.log(`render(): await page.close()`);
    await page.close();
    // console.log(`render(): browser.disconnect()`);
    browser.disconnect();

    // console.log(`render() completed`);
    return screenshot;
}