import React, { useEffect, useLayoutEffect, useRef } from 'react';
import mirrorsharp, { MirrorSharpOptions, MirrorSharpConnectionState, MirrorSharpInstance, MirrorSharpSlowUpdateResult } from 'mirrorsharp-codemirror-6-preview';
import { useRecoilValue } from 'recoil';
import { languageOptionState } from '../../shared/state/languageOptionState';
import type { Result } from '../../shared/resultTypes';
import { useServerOptions } from '../../structure/code/internal/useServerOptions';
import { useServiceUrl } from '../../structure/code/internal/useServiceUrl';
import type { ServerOptions } from '../../structure/code/internal/ServerOptions';
import { loadedCodeState } from '../../shared/state/loadedCodeState';

type ResultData = Result['value'];

type Props = {
    onSlowUpdateWait: () => void;
    onSlowUpdateResult: (value: MirrorSharpSlowUpdateResult<ResultData>) => void;
    onConnectionChange: (state: MirrorSharpConnectionState) => void;
    onCodeChange: (getCode: () => string) => void;
    onServerError: (message: string) => void;
};

const useUpdatingRef = <T, >(value: T) => {
    const ref = useRef<T>(value);
    useEffect(() => { ref.current = value; }, [value]);
    return ref;
};

export const PreviewCodeEditor: React.FC<Props> = ({
    onSlowUpdateWait,
    onSlowUpdateResult,
    onConnectionChange,
    onCodeChange,
    onServerError
}) => {
    const language = useRecoilValue(languageOptionState);
    const loadedCode = useRecoilValue(loadedCodeState);

    const serviceUrl = useServiceUrl();
    const serverOptions = useServerOptions({ initialCached: true });
    const containerRef = useRef<HTMLDivElement>(null);

    const onSlowUpdateWaitRef = useUpdatingRef(onSlowUpdateWait);
    const onSlowUpdateResultRef = useUpdatingRef(onSlowUpdateResult);
    const onConnectionChangeRef = useUpdatingRef(onConnectionChange);
    const onCodeChangeRef = useUpdatingRef(onCodeChange);
    const onServerErrorRef = useUpdatingRef(onServerError);

    const instanceRef = useRef<MirrorSharpInstance<ServerOptions>>();

    const optionsRef = useRef<MirrorSharpOptions<ServerOptions, ResultData>>({
        serviceUrl,
        language,
        initialText: loadedCode,
        initialServerOptions: serverOptions,
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
        const container = containerRef.current!;
        instanceRef.current = mirrorsharp(container, optionsRef.current);

        return () => {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            instanceRef.current!.destroy({});
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
        if (serverOptions === optionsRef.current.initialServerOptions)
            return;
        optionsRef.current = { ...optionsRef.current, initialServerOptions: serverOptions };
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        instanceRef.current.setServerOptions(serverOptions);
    }, [serverOptions]);

    return <div className="cm6-preview" ref={containerRef}>
        <small className="disclaimer">
            The preview editor is very incomplete. Most things (e.g. dark mode) will not work.
        </small>
    </div>;
};