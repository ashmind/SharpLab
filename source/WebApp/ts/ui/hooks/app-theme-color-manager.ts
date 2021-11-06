import { getEffectiveTheme, watchEffectiveTheme } from '../../helpers/theme';
import { allHooks } from './registry';

allHooks.main.ready.push(vue => {
    const head = document.getElementsByTagName('head')[0];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const colorPropertyName = head.dataset.themeColorManagerBind!;

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const meta = document.querySelector('meta[name="theme-color"]')!;
    const favicons = Array.from(document.querySelectorAll('link[rel=icon]')) as ReadonlyArray<HTMLLinkElement>;

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const defaultColor = meta.getAttribute('content')!;
    const darkColor = '#2d2d30'; // TODO: grab from CSS?

    let faviconSvg: HTMLLinkElement|undefined;
    let faviconSvgUrl: string|undefined;
    const faviconsBySizes = {} as {
        [size: string]: HTMLLinkElement;
    };
    const cacheDefault = {} as {
        svg?: string;
        [size: string]: string|undefined;
    };
    const cache = { [defaultColor]: cacheDefault } as {
        [color: string]: (typeof cacheDefault)|undefined;
    };

    for (const favicon of favicons) {
        if (favicon.getAttribute('type') === 'image/svg+xml') {
            faviconSvg = favicon;
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            faviconSvgUrl = favicon.getAttribute('href')!;
            cacheDefault.svg = faviconSvgUrl;
            continue;
        }
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const size = favicon.getAttribute('sizes')!.match(/^\d+/)![0];
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        cacheDefault[size] = favicon.getAttribute('href')!;
        faviconsBySizes[size] = favicon;
    }

    const loadImage = (src: string) => {
        const img = new Image();
        const promise = new Promise<HTMLImageElement>(resolve => {
            img.onload = () => resolve(img);
        });
        img.src = src;
        return promise;
    };

    const generateDataUrls = async (color: string) => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const recoloredSvgUrl = faviconSvgUrl!.replace(encodeURIComponent(defaultColor), encodeURIComponent(color));
        const urls = { svg: recoloredSvgUrl } as {
            [key: string]: string;
        };
        await Promise.all(Object.keys(faviconsBySizes).map(async size => {
            const canvas = document.createElement('canvas');
            canvas.width = parseInt(size, 10);
            canvas.height = parseInt(size, 10);
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            const context = canvas.getContext('2d')!;
            // Firefox bug #700533, SVG needs specific dimensions
            const finalSvgUrl = recoloredSvgUrl.replace('viewBox', encodeURIComponent(`width="${size}" height="${size}" viewBox`));

            const img = await loadImage(finalSvgUrl);
            context.drawImage(img, 0, 0);
            urls[size] = canvas.toDataURL('image/png');
        }));
        return urls;
    };

    let effectiveTheme = getEffectiveTheme();
    const applyThemeColor = (color?: string) => {
        const themeColor = (effectiveTheme !== 'dark') ? color : darkColor;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        meta.setAttribute('content', themeColor!);
    };

    let generatingColor;
    const applyFaviconColor = async (color: string) => {
        let urls = cache[color];
        if (!urls) {
            generatingColor = color;
            urls = await generateDataUrls(color);
            cache[color] = urls;
            if (color !== generatingColor) // changed while we were awaiting urls
                return;
        }
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        faviconSvg!.href = urls.svg!;
        for (const size in faviconsBySizes) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            faviconsBySizes[size].setAttribute('href', urls[size]!);
        }
    };

    if (effectiveTheme === 'dark')
        applyThemeColor();

    let lastNonDarkColor = defaultColor;
    // eslint-disable-next-line @typescript-eslint/no-misused-promises
    vue.$watch(colorPropertyName, async (color: string) => {
        if (lastNonDarkColor === color)
            return; // might be true for the first call
        lastNonDarkColor = color;
        applyThemeColor(color);
        await applyFaviconColor(color);
    }, { immediate: true });
    watchEffectiveTheme(t => {
        effectiveTheme = t;
        applyThemeColor(lastNonDarkColor);
    });
});