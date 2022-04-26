import { useEffect, useRef } from 'react';
import type { HighlightedRange } from 'ts/types/highlighted-range';

export const useSyncHighlightedRangeToMarker = (range: HighlightedRange | null, cm: CodeMirror.Editor | undefined) => {
    const markerRef = useRef<CodeMirror.TextMarker | null>(null);

    useEffect(() => {
        if (markerRef.current) {
            markerRef.current.clear();
            markerRef.current = null;
        }

        if (!range || !cm)
            return;

        const from = typeof range.start === 'object' ? range.start : cm.posFromIndex(range.start);
        const to   = typeof range.end === 'object'   ? range.end : cm.posFromIndex(range.end);
        markerRef.current = cm.markText(from, to, { className: 'highlighted' });
    }, [range, cm]);
};