import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import mirrorsharp, { MirrorSharpOptions, MirrorSharpConnectionState, MirrorSharpInstance } from 'mirrorsharp-codemirror-6-preview';
import { useRecoilValue } from 'recoil';
import type { MirrorSharpSlowUpdateResult as StableMirrorSharpSlowUpdateResult } from 'mirrorsharp';
import type { EditorView } from '@codemirror/view';
import { languageOptionState } from '../../shared/state/languageOptionState';
import type { Flow, FlowArea, Result } from '../../shared/resultTypes';
import { useServerOptions } from '../../structure/code/internal/useServerOptions';
import { useServiceUrl } from '../../structure/code/internal/useServiceUrl';
import type { ServerOptions } from '../../structure/code/internal/ServerOptions';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { effectiveThemeSelector } from '../dark-mode/themeState';
import { defaultCodeSelector, isDefaultCode } from '../../shared/state/defaultCodeSelector';
import { jumpArrows } from './extensions/jumpArrows';
import { useRenderExecutionFlowForPreview } from './execution-flow/useRenderExecutionFlowForPreview';

type ResultData = Result['value'];

type Props = {
    initialCached: boolean;
    executionFlow: Flow | null;
    // Test/Storybook only (for now)
    initialExecutionFlowSelectRule?: (area: FlowArea) => number | null;

    onSlowUpdateWait: () => void;
    onSlowUpdateResult: (value: StableMirrorSharpSlowUpdateResult<ResultData>) => void;
    onConnectionChange: (state: MirrorSharpConnectionState) => void;
    onCodeChange: (getCode: () => string) => void;
    onServerError: (message: string) => void;
};
export { Props as PreviewCodeEditorProps };

const useUpdatingRef = <T, >(value: T) => {
    const ref = useRef<T>(value);
    useEffect(() => { ref.current = value; }, [value]);
    return ref;
};

export const PreviewCodeEditor: React.FC<Props> = ({
    initialCached,

    executionFlow,
    initialExecutionFlowSelectRule,

    onSlowUpdateWait,
    onSlowUpdateResult,
    onConnectionChange,
    onCodeChange,
    onServerError
}) => {
    const language = useRecoilValue(languageOptionState);
    const loadedCode = useRecoilValue(loadedCodeState);
    const defaultCode = useRecoilValue(defaultCodeSelector);

    const serviceUrl = useServiceUrl();
    const serverOptions = useServerOptions({ initialCached: true });

    const theme = useRecoilValue(effectiveThemeSelector);

    const containerRef = useRef<HTMLDivElement>(null);

    const onSlowUpdateWaitRef = useUpdatingRef(onSlowUpdateWait);
    const onSlowUpdateResultRef = useUpdatingRef(onSlowUpdateResult);
    const onConnectionChangeRef = useUpdatingRef(onConnectionChange);
    const onServerErrorRef = useUpdatingRef(onServerError);

    const instanceRef = useRef<MirrorSharpInstance<ServerOptions>>();
    const [codeMirrorView, setCodeMirrorView] = useState<EditorView | null>(null);

    const initialConnectionRequestedRef = useRef(!initialCached);

    const optionsRef = useRef<MirrorSharpOptions<ServerOptions, ResultData>>({
        serviceUrl,
        language,
        text: loadedCode,
        serverOptions,
        theme,
        disconnected: initialCached,
        on: {
            slowUpdateWait: () => onSlowUpdateWaitRef.current(),
            slowUpdateResult: ({ diagnostics, extensionResult }) => onSlowUpdateResultRef.current({
                diagnostics,
                x: extensionResult
            }),
            connectionChange: s => onConnectionChangeRef.current(
                s === 'open' ? 'open' : 'close'
            ),
            textChange: t => onCodeChangeRef.current(t),
            serverError: e => onServerErrorRef.current(e)
        },
        codeMirror: {
            extensions: [jumpArrows]
        }
    });

    const connectIfInitialWasCached = useCallback(() => {
        if (initialConnectionRequestedRef.current)
            return;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        instanceRef.current!.connect();
        initialConnectionRequestedRef.current = true;
    }, [instanceRef]);

    const onCodeChangeRef = useUpdatingRef(useCallback((getCode: () => string) => {
        connectIfInitialWasCached();
        onCodeChange(getCode);
    }, [connectIfInitialWasCached, onCodeChange]));

    useLayoutEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const container = containerRef.current!;
        instanceRef.current = mirrorsharp(container, optionsRef.current);
        setCodeMirrorView(instanceRef.current.getCodeMirrorView());

        return () => {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            instanceRef.current!.destroy();
            setCodeMirrorView(null);
        };
    }, []);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (language === optionsRef.current.language)
            return;
        optionsRef.current = { ...optionsRef.current, language };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instanceRef.current.setLanguage(language);
    }, [language]);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (serverOptions === optionsRef.current.serverOptions)
            return;
        optionsRef.current = { ...optionsRef.current, serverOptions };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instanceRef.current.setServerOptions(serverOptions);
    }, [serverOptions]);

    useEffect(() => {
        if (!instanceRef.current)
            return;
        if (theme === optionsRef.current.theme)
            return;
        optionsRef.current = { ...optionsRef.current, theme };
        instanceRef.current.setTheme(theme);
    }, [theme]);

    useEffect(() => {
        if (!instanceRef.current)
            return;

        const code = instanceRef.current.getText();
        if (code !== defaultCode && isDefaultCode(code))
            instanceRef.current.setText(defaultCode);
    }, [defaultCode]);

    useRenderExecutionFlowForPreview(codeMirrorView, executionFlow, initialExecutionFlowSelectRule);

    return <div className="cm6-preview" ref={containerRef}>
        <small className="disclaimer">
            The preview editor is incomplete. Some things might not work as expected.
        </small>
    </div>;
};