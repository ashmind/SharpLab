import React, { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { debounce } from 'throttle-debounce';
import CodeMirror from 'codemirror';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';
import '../../shared/codemirror/mode-cil';
import '../../shared/codemirror/mode-asm';
import '../../shared/codemirror/addon-cil-infotip';
import { useSetRecoilState } from 'recoil';
import { TargetLanguageName, TARGET_ASM, TARGET_CSHARP, TARGET_IL, TARGET_RUN_IL, TARGET_VB } from '../../shared/targets';
import { codeRangeSyncSourceState } from '../../features/code-range-sync/codeRangeSyncSourceState';
import type { LinkedCodeRange } from '../../features/code-range-sync/LinkedCodeRange';
import { findRangeByTargetPosition } from '../../features/code-range-sync/findRangeByTargetPosition';
import { assertType } from '../../shared/helpers/assertType';

type Props = {
    code: string;
    language: TargetLanguageName;
    ranges: ReadonlyArray<LinkedCodeRange> | undefined;
};
export { LinkedCodeRange };

const modeMap = {
    [TARGET_CSHARP]: 'text/x-csharp',
    [TARGET_VB]:     'text/x-vb',
    [TARGET_IL]:     'text/x-cil',
    [TARGET_RUN_IL]: 'text/x-cil',
    [TARGET_ASM]:    'text/x-asm'
};
assertType<{ [K in TargetLanguageName]: string }>(modeMap);

export const CodeView: React.FC<Props> = ({ code, language, ranges }) => {
    const setSourceRange = useSetRecoilState(codeRangeSyncSourceState);

    const cmRef = useRef<CodeMirror.EditorFromTextArea | null>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const [cursorPosition, setCursorPosition] = useState<CodeMirror.Position>();
    const [cursorRange, setCursorRange] = useState<LinkedCodeRange | null>(null);
    const [hoverPosition, setHoverPosition] = useState<CodeMirror.Position>();
    const [hoverRange, setHoverRange] = useState<LinkedCodeRange | null>(null);

    const selectedRange = cursorRange ?? hoverRange;
    const selectedRangeMarkerRef = useRef<CodeMirror.TextMarker>();

    const delayedHover = useMemo(() => {
        const hover = (x: number, y: number) => setHoverPosition(
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            cmRef.current!.coordsChar({ left: x, top: y })
        );
        return debounce(50, false, hover);
    }, []);

    useLayoutEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const textarea = textareaRef.current!;
        textarea.value = code;
        const options = {
            readOnly: true,
            indentUnit: 4,
            mode: modeMap[language],
            infotip: {}
        };
        const cm = CodeMirror.fromTextArea(textarea, options);

        const wrapper = cm.getWrapperElement();
        wrapper.classList.add('mirrorsharp-theme');

        const codeElement = wrapper.getElementsByClassName('CodeMirror-code')[0] as unknown as ElementContentEditable;
        if (codeElement.contentEditable) { // HACK, mobile only
            codeElement.contentEditable = false as unknown as string;
        }

        cm.on('cursorActivity', () => setCursorPosition(cm.getCursor()));
        CodeMirror.on(wrapper, 'mousemove', (e: MouseEvent) => delayedHover(e.pageX, e.pageY));

        cmRef.current = cm;

        return () => cm.toTextArea();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => cmRef.current?.setOption('mode', modeMap[language]), [language]);
    useEffect(() => {
        const cm = cmRef.current;
        if (!cm)
            return;
        if (cm.getValue() === code)
            return;
        cm.setValue(code);
    }, [code]);

    const tryFindRange = useCallback((location: CodeMirror.Position | undefined) =>
        (ranges && location) ? findRangeByTargetPosition(ranges, location) : null,
    [ranges]);
    useEffect(() => setCursorRange(tryFindRange(cursorPosition)), [tryFindRange, cursorPosition]);
    useEffect(() => setHoverRange(tryFindRange(hoverPosition)), [tryFindRange, hoverPosition]);

    useEffect(() => {
        selectedRangeMarkerRef.current?.clear();
        if (!selectedRange)
            return;

        selectedRangeMarkerRef.current = cmRef.current?.markText(
            selectedRange.result.start, selectedRange.result.end, { className: 'highlighted' }
        );
    }, [selectedRange]);

    useEffect(
        () => setSourceRange(selectedRange?.source ?? null),
        [selectedRange, setSourceRange]
    );

    return <div>
        <textarea ref={textareaRef}></textarea>
    </div>;
};