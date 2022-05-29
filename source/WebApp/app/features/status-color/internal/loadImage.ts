export const loadImage = (src: string) => {
    const img = new Image();
    const promise = new Promise<HTMLImageElement>(resolve => {
        img.onload = () => resolve(img);
    });
    img.src = src;
    return promise;
};