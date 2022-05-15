import React, { FC, useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import mirrorsharp, { MirrorSharpConnectionState, MirrorSharpInstance, MirrorSharpOptions, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import '../shared/codemirror/addon-jump-arrows';
import { useRecoilValue } from 'recoil';
import type { Result, FlowStep } from '../../ts/types/results';
import type { ServerOptions } from '../../ts/types/server-options';
import { languageOptionState } from '../shared/state/languageOptionState';
import { useEditorCodeRangeSync } from '../features/code-range-sync/useEditorCodeRangeSync';
import { useRenderExecutionFlow } from './code-editor/useRenderExecutionFlow';
import { useServerOptions } from './code-editor/useServerOptions';
import { useServiceUrl } from './code-editor/useServiceUrl';

type ResultData = Result['value'];

type Props = {
    initialCode: string;
    initialCached: boolean;
    executionFlow: ReadonlyArray<FlowStep> | null;

    onSlowUpdateWait: () => void;
    onSlowUpdateResult: (value: MirrorSharpSlowUpdateResult<ResultData>) => void;
    onConnectionChange: (state: MirrorSharpConnectionState) => void;
    onCodeChange: (getCode: () => string) => void;
    onServerError: (message: string) => void;
};
export { Props as CodeEditorProps };

const useUpdatingRef = <T, >(value: T) => {
    const ref = useRef<T>(value);
    useEffect(() => { ref.current = value; }, [value]);
    return ref;
};

export const StableCodeEditor: FC<Props> = ({
    initialCode,
    initialCached,
    executionFlow,

    onSlowUpdateWait,
    onSlowUpdateResult,
    onConnectionChange,
    onCodeChange,
    onServerError
}) => {
    const language = useRecoilValue(languageOptionState);
    const serviceUrl = useServiceUrl();
    const serverOptions = useServerOptions({ initialCached });

    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const lastInitialCodeRef = useRef<string>();

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
        textarea.value = initialCode;
        lastInitialCodeRef.current = initialCode;

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
        if (!instance || lastInitialCodeRef.current === initialCode)
            return;
        if (instance.getCodeMirror().getValue() === lastInitialCodeRef.current)
            instance.setText(initialCode);
        lastInitialCodeRef.current = initialCode;
    }, [instance, initialCode]);

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
    useRenderExecutionFlow(executionFlow, cm);

    return <textarea ref={textareaRef}></textarea>;
};