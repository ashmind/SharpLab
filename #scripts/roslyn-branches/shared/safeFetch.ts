import fetch, { RequestInit, Response } from 'node-fetch';

export { Response };
export type SafeFetchError = Error & { response: Response };

export async function safeFetch(url: string, init?: RequestInit) {
    const response = await fetch(url, init);
    if (response.status >= 400) {
        const error = new Error(`${response.status} ${response.statusText}:\n${await response.text()}`);
        (error as SafeFetchError).response = response;
        throw error;
    }

    return response;
}