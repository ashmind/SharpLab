import { useEffect, useRef } from 'react';
import { useRecoilValue, useSetRecoilState } from 'recoil';
import { codeRangeSyncSourceState } from './codeRangeSyncSourceState';
import { codeRangeSyncTargetState } from './codeRangeSyncTargetState';

export const useEditorCodeRangeSync = (cm: CodeMirror.Editor | undefined) => {
    const range = useRecoilValue(codeRangeSyncSourceState);
    const setTargetOffset = useSetRecoilState(codeRangeSyncTargetState);

    useEffect(() => {
        if (!cm)
            return;

        const onCursorActivity = () => setTargetOffset(cm.indexFromPos(cm.getCursor()));
        cm.on('cursorActivity', onCursorActivity);
        return () => cm.off('cursorActivity', onCursorActivity);
    }, [cm, setTargetOffset]);

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