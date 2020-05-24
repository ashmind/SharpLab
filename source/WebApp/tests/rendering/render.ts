import path from 'path';
import fs from 'fs';
import puppeteer, { Page } from 'puppeteer';

function getCachePath(url: string) {
    return path.join(__dirname, '__request_cache__', url.replace(/[^a-z._=+-]/ig, '_') + '.json');
}

function isDataRequest(url: string) {
    return url.startsWith('data:');
}

async function setupRequestInterception(page: Page) {
    await page.setRequestInterception(true);

    const cachedRequests = new Set();
    const unfinishedRequests = new Set();
    page.on('request', request => {
        const method = request.method();
        const url = request.url();
        if (method !== 'GET') {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            request.abort();
            return;
        }

        unfinishedRequests.add(request);
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
            readonly headers: puppeteer.Headers;
            readonly body: string;
        };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        request.respond({
            headers: json.headers,
            body: Buffer.from(json.body, 'base64')
        });
    });

    page.on('requestfinished', request => {
        const method = request.method();
        const url = request.url();

        unfinishedRequests.delete(request);

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
            while (unfinishedRequests.size > 0) {
                await new Promise(resolve => setTimeout(resolve, 100));
            }
        }
    };
}

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
    const content = `<!DOCTYPE html><html><head></head><body class="${bodyClass}">${html}</body></html>`;

    const browser = await puppeteer.launch({ headless: !debug });
    const page = await browser.newPage();

    const { waitForUnfinishedRequests } = await setupRequestInterception(page);

    await page.setViewport({ width, height });
    await page.setContent(content);
    for (const style of styles) {
        await page.addStyleTag(style);
    }
    await waitForUnfinishedRequests();

    const screenshot = await page.screenshot();
    if (debug)
        debugger; // eslint-disable-line no-debugger

    await browser.close();

    return screenshot;
}