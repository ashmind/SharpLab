import { decodeArrayBufferFromBase64 } from '../helpers/array-buffer';
import type { CachedUpdateResult } from '../types/results';

const { host } = window.location;
const cacheCdnBaseUrl = host.endsWith('sharplab.io')
    ? (`https://slpublic.azureedge.net/cache/` + (host !== 'sharplab.io' ? 'edge' : 'main'))
    : `http://127.0.0.1:10000/devstoreaccount1/cache/edge`;

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
    encrypted: Encrypted;
};

// https://stackoverflow.com/questions/40031688/javascript-arraybuffer-to-hex#answer-53307879
const encodeArrayBufferToHex = (buffer: ArrayBuffer) => {
    const map = '0123456789abcdef';
    return [...(new Uint8Array(buffer))]
        .map(v => map[v >> 4] + map[v & 15])
        .join('');
};

export const buildCacheKeysAsync = async ({ language, target, release, branchId, code }: CacheKeyData) => {
    const keyFormat = `${language}|${target}|${release ? 'release' : 'debug'}|${branchId ?? ''}|${code}`;
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

export const loadResultFromCacheAsync = async (keyData: CacheKeyData): Promise<CachedUpdateResult | null> => {
    const { cacheKey, secretKey } = await buildCacheKeysAsync(keyData);
    const response = await fetch(`${cacheCdnBaseUrl}/${keyData.branchId ?? 'default'}/${cacheKey}.json`);
    if (response.status === 404)
        return null;

    if (response.status !== 200)
        throw new Error(`Unexpected CDN response code: ${response.status} ${response.statusText}`);

    const json = await response.json() as CacheData;

    const data = await decryptCacheDataAsync(json.encrypted, secretKey);
    return {
        ...(JSON.parse(data) as Omit<CachedUpdateResult, 'cached'>),
        cached: true
    };
};