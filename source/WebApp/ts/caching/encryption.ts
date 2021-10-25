export const generateCacheEncryptionKeyAsync = async () => {
    const key = await crypto.subtle.generateKey({
        name: 'AES-GCM',
        length: 256
    }, true, ['encrypt', 'decrypt']);
    const exported = await crypto.subtle.exportKey('raw', key);

    return { key, exported } as const;
};