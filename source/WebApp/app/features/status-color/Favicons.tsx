import React, { useEffect, useMemo } from 'react';
import { useRecoilValue } from 'recoil';
import { replacedHeadElements } from '../../helpers/DocumentHead';
import { statusColorSelector, DEFAULT_STATUS_COLOR } from './internal/statusColorSelector';
import type { FaviconsData } from './internal/FaviconsData';
import { RecolorArguments, useRecoloredFaviconsData } from './internal/useRecoloredFaviconData';

const initial: FaviconsData = (() => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const svgUrl = replacedHeadElements
        .find(e => e.getAttribute('type') === 'image/svg+xml')!
        .getAttribute('href')!;

    const sizes = replacedHeadElements
        .map(e => ({ sizes: e.getAttribute('sizes'), url: e.getAttribute('href') }))
        .filter(i => i.sizes)
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        .map(({ sizes, url }) => ({ size: parseInt(sizes!.split('x')[0], 10), url: url! }));

    return { svgUrl, sizes };
})();

type Props = {
    // Test/Storybook only
    onRecolorComplete?: () => void;
};

export const Favicons: React.FC<Props> = ({ onRecolorComplete }) => {
    const color = useRecoilValue(statusColorSelector);
    const recolorArguments = useMemo<RecolorArguments>(() => ({
        initial: {
            svgDataUrl: initial.svgUrl,
            color: DEFAULT_STATUS_COLOR
        },
        color,
        sizes: initial.sizes.map(s => s.size)
    }), [color]);
    const recolored = useRecoloredFaviconsData(recolorArguments);
    useEffect(() => onRecolorComplete?.(), [onRecolorComplete, recolored]);

    const current = recolored ?? initial;

    return <>
        <link rel="icon" type="image/svg+xml" href={current.svgUrl} />
        {current.sizes.map(({ size, url }) => <link
            key={size}
            rel="icon"
            type="image/png"
            href={url}
            sizes={`${size}x${size}`}
        />)}
    </>;
};