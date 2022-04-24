import React, { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { debounce } from 'throttle-debounce';
import CodeMirror from 'codemirror';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';
import 'components/internal/codemirror/mode-cil';
import 'components/internal/codemirror/mode-asm';
import 'components/internal/codemirror/addon-cil-infotip';
import { targets } from 'ts/helpers/targets';
import type { LinkedRange } from './code/LinkedRange';
import { findRange } from './code/findRange';

type TargetLanguageName = typeof targets.csharp|typeof targets.vb|typeof targets.il|typeof targets.asm;

type Props = {
    code: string;
    language: TargetLanguageName;
    ranges: ReadonlyArray<LinkedRange> | undefined;
    onRangeSelect: (range: LinkedRange | null) => void;
};

const modeMap = {
    [targets.csharp]: 'text/x-csharp',
    [targets.vb]:     'text/x-vb',
    [targets.il]:     'text/x-cil',
    [targets.asm]:    'text/x-asm'
};

export const CodeView: React.FC<Props> = ({ code, language, ranges, onRangeSelect }) => {
    const cmRef = useRef<CodeMirror.EditorFromTextArea | null>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const [cursorPosition, setCursorPosition] = useState<CodeMirror.Position>();
    const [cursorRange, setCursorRange] = useState<LinkedRange | null>(null);
    const [hoverPosition, setHoverPosition] = useState<CodeMirror.Position>();
    const [hoverRange, setHoverRange] = useState<LinkedRange | null>(null);

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
        ranges && location ? findRange(ranges, location) : null,
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

    useEffect(() => onRangeSelect(selectedRange), [onRangeSelect, selectedRange]);

    return <div>
        <textarea ref={textareaRef}></textarea>
    </div>;
};