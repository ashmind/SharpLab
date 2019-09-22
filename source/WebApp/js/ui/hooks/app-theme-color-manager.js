import { getEffectiveTheme, watchEffectiveTheme } from '../../helpers/theme.js';
import registry from './registry.js';

registry.main.ready.push(vue => {
    const head = document.getElementsByTagName('head')[0];
    const colorPropertyName = head.dataset.themeColorManagerBind;

    const meta = document.querySelector('meta[name="theme-color"]');
    const favicons = Array.from(document.querySelectorAll('link[rel=icon]'));

    const defaultColor = meta.getAttribute('content');
    const darkColor = '#2d2d30'; // TODO: grab from CSS?

    let faviconSvg;
    let faviconSvgUrl;
    const faviconsBySizes = {};
    const cacheDefault = {};
    const cache = { [defaultColor]: cacheDefault };

    for (const favicon of favicons) {
        if (favicon.getAttribute('type') === 'image/svg+xml') {
            faviconSvg = favicon;
            faviconSvgUrl = favicon.getAttribute('href');
            cacheDefault.svg = faviconSvgUrl;
            continue;
        }
        const size = favicon.getAttribute('sizes').match(/^\d+/)[0];
        cacheDefault[size] = favicon.getAttribute('href');
        faviconsBySizes[size] = favicon;
    }

    const loadImage = src => {
        const img = new Image();
        const promise = new Promise(resolve => { img.onload = () => resolve(img); });
        img.src = src;
        return promise;
    };

    const generateDataUrls = async color => {
        const recoloredSvgUrl = faviconSvgUrl.replace(encodeURIComponent(defaultColor), encodeURIComponent(color));
        const urls = {};
        urls.svg = recoloredSvgUrl;
        await Promise.all(Object.keys(faviconsBySizes).map(async size => {
            const canvas = document.createElement('canvas');
            canvas.width = parseInt(size);
            canvas.height = parseInt(size);
            const context = canvas.getContext('2d');
            // Firefox bug #700533, SVG needs specific dimensions
            const finalSvgUrl = recoloredSvgUrl.replace('viewBox', encodeURIComponent(`width="${size}" height="${size}" viewBox`));

            const img = await loadImage(finalSvgUrl);
            context.drawImage(img, 0, 0);
            urls[size] = canvas.toDataURL('image/png');
        }));
        return urls;
    };

    let effectiveTheme = getEffectiveTheme();
    const applyThemeColor = color => {
        const themeColor = (effectiveTheme !== 'dark') ? color : darkColor;
        meta.setAttribute('content', themeColor);
    };

    let generatingColor;
    const applyFaviconColor = async color => {
        let urls = cache[color];
        if (!urls) {
            generatingColor = color;
            urls = await generateDataUrls(color);
            cache[color] = urls;
            if (color !== generatingColor) // changed while we were awaiting urls
                return;
        }
        faviconSvg.href = urls.svg;
        for (const size in faviconsBySizes) {
            faviconsBySizes[size].setAttribute('href', urls[size]);
        }
    };

    if (effectiveTheme === 'dark')
        applyThemeColor();

    let lastNonDarkColor = defaultColor;
    vue.$watch(colorPropertyName, color => {
        lastNonDarkColor = color;
        applyThemeColor(color);
        applyFaviconColor(color);
    });
    watchEffectiveTheme(t => {
        effectiveTheme = t;
        applyThemeColor(lastNonDarkColor);
    });
});