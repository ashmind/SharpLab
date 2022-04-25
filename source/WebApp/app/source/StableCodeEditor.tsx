import React, { FC, useCallback, useEffect, useLayoutEffect, useRef } from 'react';
import type { LanguageName } from 'ts/helpers/languages';
import type { HighlightedRange } from 'ts/types/highlighted-range';
import type { FlowStep, Result } from 'ts/types/results';
import mirrorsharp, { MirrorSharpConnectionState, MirrorSharpInstance, MirrorSharpOptions, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import type { ServerOptions } from 'ts/types/server-options';
import { useSyncHighlightedRangeToMarker } from './code-editor/useSyncHighlightedRangeToMarker';
import { useRenderExecutionFlow } from './code-editor/useRenderExecutionFlow';
import 'components/internal/codemirror/addon-jump-arrows';

type ResultData = Result['value'];

type Props = {
    initialText: string;
    initialCached: boolean;
    serviceUrl: string;
    language: LanguageName;
    serverOptions: ServerOptions;
    highlightedRange: HighlightedRange | null;
    executionFlow: ReadonlyArray<FlowStep> | null;

    onSlowUpdateWait: () => void;
    onSlowUpdateResult: (value: MirrorSharpSlowUpdateResult<ResultData>) => void;
    onConnectionChange: (state: MirrorSharpConnectionState) => void;
    onTextChange: (getText: () => string) => void;
    onCursorMove: (getOffset: () => number) => void;
    onServerError: (message: string) => void;
};
export { Props as CodeEditorProps };

const useUpdatingRef = <T, >(value: T) => {
    const ref = useRef<T>(value);
    useEffect(() => { ref.current = value; }, [value]);
    return ref;
};

export const StableCodeEditor: FC<Props> = ({
    initialText,
    initialCached,
    serviceUrl,
    language,
    serverOptions,
    highlightedRange,
    executionFlow,

    onSlowUpdateWait,
    onSlowUpdateResult,
    onConnectionChange,
    onTextChange,
    onCursorMove,
    onServerError
}) => {
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const lastInitialTextRef = useRef<string>();

    const onSlowUpdateWaitRef = useUpdatingRef(onSlowUpdateWait);
    const onSlowUpdateResultRef = useUpdatingRef(onSlowUpdateResult);
    const onConnectionChangeRef = useUpdatingRef(onConnectionChange);
    const onCursorMoveRef = useUpdatingRef(onCursorMove);
    const onServerErrorRef = useUpdatingRef(onServerError);

    const instanceRef = useRef<MirrorSharpInstance<ServerOptions>>();

    const initialConnectionRequestedRef = useRef(!initialCached);

    const connectIfInitialWasCachedRef = useRef(() => {
        if (initialConnectionRequestedRef.current)
            return;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        instanceRef.current!.connect();
        initialConnectionRequestedRef.current = true;
    });

    const onTextChangeRef = useUpdatingRef(useCallback((getText: () => string) => {
        connectIfInitialWasCachedRef.current();
        onTextChange(getText);
    }, [onTextChange]));

    const optionsRef = useRef<MirrorSharpOptions<ServerOptions, ResultData>>({
        serviceUrl,
        language,
        initialServerOptions: serverOptions,
        noInitialConnection: initialCached,
        on: {
            slowUpdateWait: () => onSlowUpdateWaitRef.current(),
            slowUpdateResult: r => onSlowUpdateResultRef.current(r),
            connectionChange: s => onConnectionChangeRef.current(s),
            textChange: t => onTextChangeRef.current(t),
            serverError: e => onServerErrorRef.current(e)
        }
    });

    useLayoutEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const textarea = textareaRef.current!;
        textarea.value = initialText;
        lastInitialTextRef.current = initialText;

        const instance = mirrorsharp(textarea, optionsRef.current);
        instanceRef.current = instance;

        const cm = instance.getCodeMirror();
        const contentEditable = cm
            .getWrapperElement()
            .querySelector('[contentEditable=true]');
        if (contentEditable)
            contentEditable.setAttribute('autocomplete', 'off');

        const getCursorOffset = () => cm.indexFromPos(cm.getCursor());
        cm.on('cursorActivity', () => onCursorMoveRef.current(getCursorOffset));

        return () => {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            instanceRef.current!.destroy({ keepCodeMirror: true });
            const wrapper = cm.getWrapperElement();
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            wrapper.parentElement!.removeChild(wrapper);
        };
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (lastInitialTextRef.current === initialText)
            return;
        lastInitialTextRef.current = initialText;
        instanceRef.current.setText(initialText);
    }, [initialText]);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (language === optionsRef.current.language)
            return;
        optionsRef.current = { ...optionsRef.current, language };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instanceRef.current.setLanguage(language);
        connectIfInitialWasCachedRef.current();
    }, [language]);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (serverOptions === optionsRef.current.initialServerOptions)
            return;
        optionsRef.current = { ...optionsRef.current, initialServerOptions: serverOptions };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instanceRef.current.setServerOptions(serverOptions);
        connectIfInitialWasCachedRef.current();
    }, [serverOptions]);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (serviceUrl === optionsRef.current.serviceUrl)
            return;
        optionsRef.current = { ...optionsRef.current, serviceUrl, noInitialConnection: false };
        instanceRef.current.destroy({ keepCodeMirror: true });

        initialConnectionRequestedRef.current = true;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        instanceRef.current = mirrorsharp(textareaRef.current!, optionsRef.current);
    }, [serviceUrl]);

    const cm = instanceRef.current?.getCodeMirror();
    useSyncHighlightedRangeToMarker(highlightedRange, cm);
    useRenderExecutionFlow(executionFlow, cm);

    return <textarea ref={textareaRef}></textarea>;
};