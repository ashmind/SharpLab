import Vue from 'vue';

Vue.component('app-favicon-manager', {
    props: {
        color: String,
        defaultColor: String
    },
    mounted: function() {
        const favicons = Array.from(document.querySelectorAll('link[rel=icon]'));
        let faviconSvg;
        let faviconSvgUrl;
        const faviconsBySizes = {};
        const cacheDefault = {};
        const cache = { [this.defaultColor]: cacheDefault };

        for (let favicon of favicons) {
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
            const promise = new Promise(resolve => img.onload = () => resolve(img));
            img.src = src;
            return promise;
        };

        const generateDataUrls = async color => {
            const recoloredSvgUrl = faviconSvgUrl.replace(encodeURIComponent(this.defaultColor), encodeURIComponent(color));
            const urls = {};
            urls.svg = recoloredSvgUrl;
            await Promise.all(Object.keys(faviconsBySizes).map(async size => {
                const canvas = document.createElement('canvas');
                canvas.width = size;
                canvas.height = size;
                const context = canvas.getContext('2d');
                // Firefox bug #700533, SVG needs specific dimensions
                const finalSvgUrl = recoloredSvgUrl.replace('viewBox', encodeURIComponent(`width="${size}" height="${size}" viewBox`));

                const img = await loadImage(finalSvgUrl);
                context.drawImage(img, 0, 0);
                urls[size] = canvas.toDataURL('image/png');
            }));
            return urls;
        }

        this.$watch('color', async color => {
            var urls = cache[color];
            if (!urls) {
                urls = await generateDataUrls(color);
                cache[color] = urls;
                if (this.color !== color) // changed while we were awaiting urls
                    return;
            }
            faviconSvg.href = urls.svg;
            for (let size in faviconsBySizes) {
                faviconsBySizes[size].setAttribute('href', urls[size]);
            }
        });
    }
});