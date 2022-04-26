import React, { FC, useMemo, useState } from 'react';
import type { AppOptions } from 'ts/types/app';
import type { Branch } from 'ts/types/branch';
import type { Gist } from 'ts/types/gist';
import type { ParsedResult } from 'ts/types/results';
import { BranchSelect } from './header/BranchSelect';
import { GistManager } from './header/GistManager';
import { LanguageSelect } from './header/LanguageSelect';
import { CodeEditor, CodeEditorProps } from './code/CodeEditor';

type Props = {
    options: AppOptions;
    branches: ReadonlyArray<Branch>;
    result: ParsedResult;

    initialCode: string;
    codeEditorProps: Omit<
        CodeEditorProps,
        'initialText'|'initialCached'|'serviceUrl'|'language'|'executionFlow'|'onTextChange'
    >;

    gist: Gist;
    onGistSave: (gist: Gist) => void;
};
export { Props as CodeTopSectionProps };

export const getServiceUrl = (branch: Branch|null) => {
    const httpRoot = branch ? branch.url : window.location.origin;
    return `${httpRoot.replace(/^http/, 'ws')}/mirrorsharp`;
};

export const CodeTopSection: FC<Props> = ({
    options,
    branches,
    result,

    initialCode,
    codeEditorProps,

    gist,
    onGistSave
}) => {
    const [code, setCode] = useState(initialCode);
    const serviceUrl = useMemo(() => getServiceUrl(options.branch), [options.branch]);

    return <section className="top-section code">
        <header>
            <h1>Code</h1>
            <LanguageSelect
                language={options.language}
                onSelect={l => options.language = l}
                htmlProps={{ tabIndex: 1 }} />
            <GistManager
                className="header-block"
                gist={gist}
                context={{ code, options, result }}
                buttonProps={{ className: 'header-text-button', tabIndex: 2 }}
                onSave={onGistSave} />
            <div className="offline-only">[connection lost, reconnecting…]</div>
            <BranchSelect
                allBranches={branches}
                language={options.language}
                branch={options.branch}
                onSelect={b => options.branch = b}
                htmlProps={{ tabIndex: 3 }} />
        </header>
        <div className="content">
            <CodeEditor
                {...codeEditorProps}
                initialCode={initialCode}
                initialCached={!!result.cached}
                serviceUrl={serviceUrl}
                language={options.language}
                executionFlow={(result.type === 'run' && result.value) ? result.value.flow : null}
                onCodeChange={c => setCode(c)} />
        </div>
    </section>;
};