// RFC 4648 §5
export const toBase64Url = (base64: string) => {
    return base64.replace(/\+/g, '-').replace(/\//g, '_');
};

export const fromBase64Url = (base64Url: string) => {
    return base64Url.replace(/-/g, '+').replace(/_/g, '/');
};