import React, { useLayoutEffect, useRef, useState } from 'react';

export const ScrollBarSizeStyleSetup: React.FC = () => {
    const [done, setDone] = useState(false);
    const ref = useRef<HTMLDivElement>(null);

    useLayoutEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const scrollbarSize = ref.current!.offsetWidth - ref.current!.clientWidth;
        document.documentElement.style.setProperty('--js-scrollbar-width', scrollbarSize + 'px');
        setDone(true);
    }, []);

    if (done) return null;
    return <div ref={ref} style={{
        position: 'absolute',
        width: '100px',
        height: '100px',
        overflow: 'scroll',
        left: '-9999px'
    }} />;
};