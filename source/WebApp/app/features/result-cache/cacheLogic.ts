import { decodeArrayBufferFromBase64 } from './internal/arrayBuffer';
import type { CachedUpdateResult } from './types';

const [cacheEnvironment, cacheCdnBaseUrl] = (() => {
    const main = ['main', 'https://slpublic.azureedge.net/cache/main'] as const;
    const edge = ['edge', 'https://slpublic.azureedge.net/cache/edge'] as const;
    const local = ['edge', 'http://127.0.0.1:10000/devstoreaccount1/cache/edge'] as const;

    const { host } = window.location;
    const override = (window.location.search.match(/[?&]cache=([^?&]+)/) ?? [])[1];

    switch (host) {
        case 'sharplab.io':
            if (override)
                throw new Error('Cannot override cache environment on the main site (remove ?cache=).');
            return main;
        case 'edge.sharplab.io':
            return override === 'main' ? main : edge;
        default:
            return { main, edge }[override] ?? local;
    }
})();

const AES = 'AES-GCM';

export type CacheKeyData = {
    language: string;
    target: string;
    release: boolean;
    branchId: string | null;
    code: string;
};

type Encrypted = {
    iv: string;
    tag: string;
    data: string;
};

type CacheData = {
    version: 1;
    date: string;
    encrypted: Encrypted;
};

// https://stackoverflow.com/questions/40031688/javascript-arraybuffer-to-hex#answer-53307879
const encodeArrayBufferToHex = (buffer: ArrayBuffer) => {
    const map = '0123456789abcdef';
    return [...(new Uint8Array(buffer))]
        .map(v => map[v >> 4] + map[v & 15])
        .join('');
};

export const buildCacheKeysAsync = async ({ language, target, release, code }: CacheKeyData, branchKey: string | null) => {
    const keyFormat = `${language}|${target}|${release ? 'release' : 'debug'}|${branchKey ?? ''}|${code}`;
    const secretKeyBytes = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(keyFormat));
    const cacheKey = encodeArrayBufferToHex(await crypto.subtle.digest('SHA-256', secretKeyBytes));

    const secretKey = await crypto.subtle.importKey('raw', secretKeyBytes, { name: AES }, false, ['encrypt', 'decrypt']);

    return { cacheKey, secretKey };
};

export const decryptCacheDataAsync = async (encrypted: Encrypted, secretKey: CryptoKey): Promise<string> => {
    const dataBytes = decodeArrayBufferFromBase64(encrypted.data);
    const ivBytes = decodeArrayBufferFromBase64(encrypted.iv);
    const tagBytes = decodeArrayBufferFromBase64(encrypted.tag);

    const dataWithTagBytes = new Uint8Array(dataBytes.byteLength + tagBytes.byteLength);
    dataWithTagBytes.set(new Uint8Array(dataBytes), 0);
    dataWithTagBytes.set(new Uint8Array(tagBytes), dataBytes.byteLength);

    const resultBytes = await crypto.subtle.decrypt({
        name: AES,
        iv: ivBytes
    }, secretKey, dataWithTagBytes);
    return new TextDecoder('utf-8').decode(resultBytes);
};

const getBranchCacheId = (branchId: string | null) => {
    if (!branchId)
        return null;

    // Workaround, should probably update server to use id
    // as the cache key instead, to be defined. Will not
    // work for edge branches for now.
    const architecturePrefix = cacheEnvironment === 'main'
        ? 'sl-a-'
        : 'sl-a-edge-';
    return {
        'core-x64': architecturePrefix + 'core-x64',
        'netfx': architecturePrefix + 'netfx',
        'x64': architecturePrefix + 'netfx-x64'
    }[branchId] ?? `sl-b-dotnet-${branchId}`;
};

export const loadResultFromCacheAsync = async (keyData: CacheKeyData): Promise<CachedUpdateResult | null> => {
    const branchCacheId = getBranchCacheId(keyData.branchId);
    const { cacheKey, secretKey } = await buildCacheKeysAsync(keyData, branchCacheId);

    const response = await fetch(`${cacheCdnBaseUrl}/${branchCacheId ?? 'default'}/${cacheKey}.json`);
    if (response.status === 404)
        return null;

    if (response.status !== 200)
        throw new Error(`Unexpected CDN response code: ${response.status} ${response.statusText}`);

    const json = await response.json() as CacheData;

    const data = await decryptCacheDataAsync(json.encrypted, secretKey);
    return {
        ...(JSON.parse(data) as Omit<CachedUpdateResult, 'cached'>),
        cached: { date: new Date(json.date) }
    };
};