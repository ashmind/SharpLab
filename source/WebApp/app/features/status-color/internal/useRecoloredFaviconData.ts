import { useEffect, useRef, useState } from 'react';
import type { FaviconsData } from './FaviconsData';
import { loadImage } from './loadImage';

export type RecolorArguments = {
    initial: { svgDataUrl: string; color: string };
    color: string;
    sizes: ReadonlyArray<number>;
};

const recolor = async ({ initial, color, sizes }: RecolorArguments): Promise<FaviconsData> => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const recoloredSvgUrl = initial.svgDataUrl.replace(encodeURIComponent(initial.color), encodeURIComponent(color));
    const recoloredSizes = await Promise.all(sizes.map(async size => {
        const canvas = document.createElement('canvas');
        canvas.width = size;
        canvas.height = size;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const context = canvas.getContext('2d')!;
        // Firefox bug #700533, SVG needs specific dimensions
        const finalSvgUrl = recoloredSvgUrl.replace('viewBox', encodeURIComponent(`width="${size}" height="${size}" viewBox`));

        const img = await loadImage(finalSvgUrl);
        context.drawImage(img, 0, 0);
        const url = canvas.toDataURL('image/png');

        return { size, url };
    }));

    return {
        svgUrl: recoloredSvgUrl,
        sizes: recoloredSizes
    };
};

export const useRecoloredFaviconsData = ({ initial, color, sizes }: RecolorArguments) => {
    const cache = useRef<{ readonly [color: string]: (Readonly<FaviconsData> | undefined) }>({});
    const [result, setResult] = useState<FaviconsData | null>(null);

    useEffect(() => {
        if (color === initial.color) {
            setResult(null);
            return;
        }

        const cached = cache.current[color];
        if (cached) {
            setResult(cached);
            return;
        }

        let colorChanged = false;
        void((async () => {
            const recolored = await recolor({ initial, color, sizes });
            cache.current = { ...cache.current, [color]: recolored };
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            if (!colorChanged)
                setResult(recolored);
        })());

        return () => { colorChanged = true; };
    }, [initial, sizes, color]);

    return result;
};