import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import mirrorsharp, { MirrorSharpConnectionState, MirrorSharpInstance, MirrorSharpOptions, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import 'codemirror/mode/vb/vb';
import '../../shared/codemirror/addon-jump-arrows';
import '../../shared/codemirror/mode-cil';
import { useRecoilValue } from 'recoil';
import { useEditorCodeRangeSync } from '../../features/code-range-sync/useEditorCodeRangeSync';
import type { Result, Flow, FlowArea } from '../../shared/resultTypes';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { defaultCodeSelector, isDefaultCode } from '../../shared/state/defaultCodeSelector';
import { useRenderExecutionFlow } from '../../features/execution-flow/useRenderExecutionFlow';
import { useServerOptions } from './internal/useServerOptions';
import { useServiceUrl } from './internal/useServiceUrl';
import type { ServerOptions } from './internal/ServerOptions';

type ResultData = Result['value'];

type Props = {
    initialCached: boolean;
    executionFlow: Flow | null;

    // Test/Storybook only (for now)
    initialExecutionFlowSelectRule?: (area: FlowArea) => number | null;

    onSlowUpdateWait: () => void;
    onSlowUpdateResult: (value: MirrorSharpSlowUpdateResult<ResultData>) => void;
    onConnectionChange: (state: MirrorSharpConnectionState) => void;
    onCodeChange: (getCode: () => string) => void;
    onServerError: (message: string) => void;
};
export { Props as StableCodeEditorProps };

const useUpdatingRef = <T, >(value: T) => {
    const ref = useRef<T>(value);
    useEffect(() => { ref.current = value; }, [value]);
    return ref;
};

export const StableCodeEditor: React.FC<Props> = ({
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
    const serverOptions = useServerOptions({ initialCached });

    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const onSlowUpdateWaitRef = useUpdatingRef(onSlowUpdateWait);
    const onSlowUpdateResultRef = useUpdatingRef(onSlowUpdateResult);
    const onConnectionChangeRef = useUpdatingRef(onConnectionChange);
    const onServerErrorRef = useUpdatingRef(onServerError);

    const [instance, setInstance] = useState<MirrorSharpInstance<ServerOptions>>();
    const instanceRef = useUpdatingRef(instance);

    const initialConnectionRequestedRef = useRef(!initialCached);

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

    const optionsRef = useRef<MirrorSharpOptions<ServerOptions, ResultData>>({
        serviceUrl,
        language,
        initialServerOptions: serverOptions,
        noInitialConnection: initialCached,
        on: {
            slowUpdateWait: () => onSlowUpdateWaitRef.current(),
            slowUpdateResult: r => onSlowUpdateResultRef.current(r),
            connectionChange: s => onConnectionChangeRef.current(s),
            textChange: t => onCodeChangeRef.current(t),
            serverError: e => onServerErrorRef.current(e)
        }
    });

    useLayoutEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const textarea = textareaRef.current!;
        textarea.value = loadedCode;

        const instance = mirrorsharp(textarea, optionsRef.current);
        setInstance(instance);

        const cm = instance.getCodeMirror();
        const contentEditable = cm
            .getWrapperElement()
            .querySelector('[contentEditable=true]');
        if (contentEditable)
            contentEditable.setAttribute('autocomplete', 'off');

        return () => {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            instance.destroy({ keepCodeMirror: true });
            const wrapper = cm.getWrapperElement();
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            wrapper.parentElement!.removeChild(wrapper);
        };
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (!instance)
            return;

        const code = instance.getCodeMirror().getValue();
        if (code !== defaultCode && isDefaultCode(code))
            instance.setText(defaultCode);
    }, [instance, defaultCode]);

    useEffect(() => {
        const instance = instanceRef.current;
        if (!instance)
            return;

        const code = instance.getCodeMirror().getValue();
        if (loadedCode !== code)
            instance.setText(loadedCode);
    }, [instanceRef, loadedCode]);

    useEffect(() => {
        if (!instance || language === optionsRef.current.language)
            return;
        optionsRef.current = { ...optionsRef.current, language };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instance.setLanguage(language);
        connectIfInitialWasCached();
    }, [connectIfInitialWasCached, instance, language]);

    useEffect(() => {
        if (!instance || serverOptions === optionsRef.current.initialServerOptions)
            return;
        optionsRef.current = { ...optionsRef.current, initialServerOptions: serverOptions };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instance.setServerOptions(serverOptions);
        connectIfInitialWasCached();
    }, [instance, serverOptions, connectIfInitialWasCached]);

    useEffect(() => {
        if (!instance || serviceUrl === optionsRef.current.serviceUrl)
            return;
        optionsRef.current = { ...optionsRef.current, serviceUrl, noInitialConnection: false };
        instance.destroy({ keepCodeMirror: true });

        initialConnectionRequestedRef.current = true;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        setInstance(mirrorsharp(textareaRef.current!, optionsRef.current));
    }, [instance, serviceUrl]);

    const cm = instance?.getCodeMirror();
    useEditorCodeRangeSync(cm);
    useRenderExecutionFlow(cm, executionFlow, initialExecutionFlowSelectRule);

    return <textarea ref={textareaRef}></textarea>;
};