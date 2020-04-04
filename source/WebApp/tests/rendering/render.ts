import fs from 'fs';
import puppeteer, { Page } from 'puppeteer';

function getCachePath(url: string) {
    return __dirname + '/__request_cache__/' + url.replace(/[^a-z._=+-]/ig, '_') + '.json';
}

function isDataRequest(url: string) {
    return /^data:/.test(url);
}

function setupRequestInterception(page: Page) {
    page.setRequestInterception(true);

    const cachedRequests = new Set();
    const unfinishedRequests = new Set();
    page.on('request', request => {
        const method = request.method();
        const url = request.url();
        if (method !== 'GET') {
            request.abort();
            return;
        }

        unfinishedRequests.add(request);
        if (isDataRequest(url)) {
            request.continue();
            return;
        }

        const cachePath = getCachePath(url);
        if (!fs.existsSync(cachePath)) {
            console.log(`${method} ${url} - not cached yet`);
            request.continue();
            return;
        }

        cachedRequests.add(request);
        const json = JSON.parse(fs.readFileSync(cachePath, { encoding: 'utf-8' }));
        request.respond({
            headers: json.headers,
            body: Buffer.from(json.body, 'base64')
        });
    });

    page.on('requestfinished', async request => {
        const method = request.method();
        const url = request.url();

        unfinishedRequests.delete(request);

        if (cachedRequests.has(request) || method !== 'GET' || isDataRequest(url))
            return;

        const response = request.response();
        if (!response || response.status() !== 200)
            return;

        const data = {
            headers: response.headers(),
            body: (await response.buffer()).toString('base64')
        };

        fs.writeFileSync(getCachePath(request.url()), JSON.stringify(data, null, 4));
        console.log(`${method} ${url} - added to cache`);
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

    debug = false
}: {
    html: string;
    bodyClass?: string;
    styles?: ReadonlyArray<{ path: string }>;
    debug?: boolean;
}) {
    const content = `<!DOCTYPE html><html><head></head><body class="${bodyClass}">${html}</body></html>`;

    const browser = await puppeteer.launch({ headless: !debug });
    const page = await browser.newPage();

    const { waitForUnfinishedRequests } = setupRequestInterception(page);

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