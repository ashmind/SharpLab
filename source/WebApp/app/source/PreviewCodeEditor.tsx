import React, { FC, useEffect, useLayoutEffect, useRef } from 'react';
import type { LanguageName } from 'ts/helpers/languages';
import type { Result } from 'ts/types/results';
import mirrorsharp, { MirrorSharpOptions, MirrorSharpConnectionState, MirrorSharpInstance, MirrorSharpSlowUpdateResult } from 'mirrorsharp-codemirror-6-preview';
import type { ServerOptions } from 'ts/types/server-options';

type ResultData = Result['value'];

type Props = {
    initialText: string;
    serviceUrl: string;
    language: LanguageName;
    serverOptions: ServerOptions;

    onSlowUpdateWait: () => void;
    onSlowUpdateResult: (value: MirrorSharpSlowUpdateResult<ResultData>) => void;
    onConnectionChange: (state: MirrorSharpConnectionState) => void;
    onTextChange: (getText: () => string) => void;
    onServerError: (message: string) => void;
};

const useUpdatingRef = <T, >(value: T) => {
    const ref = useRef<T>(value);
    useEffect(() => { ref.current = value; }, [value]);
    return ref;
};

export const PreviewCodeEditor: FC<Props> = ({
    initialText,
    serviceUrl,
    language,
    serverOptions,

    onSlowUpdateWait,
    onSlowUpdateResult,
    onConnectionChange,
    onTextChange,
    onServerError
}) => {
    const containerRef = useRef<HTMLDivElement>(null);

    const onSlowUpdateWaitRef = useUpdatingRef(onSlowUpdateWait);
    const onSlowUpdateResultRef = useUpdatingRef(onSlowUpdateResult);
    const onConnectionChangeRef = useUpdatingRef(onConnectionChange);
    const onTextChangeRef = useUpdatingRef(onTextChange);
    const onServerErrorRef = useUpdatingRef(onServerError);

    const instanceRef = useRef<MirrorSharpInstance<ServerOptions>>();

    const optionsRef = useRef<MirrorSharpOptions<ServerOptions, ResultData>>({
        serviceUrl,
        language,
        initialText,
        initialServerOptions: serverOptions,
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