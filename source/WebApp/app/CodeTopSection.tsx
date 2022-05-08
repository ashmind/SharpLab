import React, { FC, ReactElement } from 'react';
import { GistManager } from './features/save-as-gist/GistManager';
import { BranchSelect } from './header/BranchSelect';
import { LanguageSelect } from './header/LanguageSelect';

type Props = {
    codeEditor: ReactElement;
};

const GIST_BUTTON_PROPS = {
    className: 'header-text-button',
    tabIndex: 2
} as const;

export const CodeTopSection: FC<Props> = ({ codeEditor }) => {
    return <section className="top-section code">
        <header>
            <h1>Code</h1>
            <LanguageSelect tabIndex={1} useAriaLabel />
            <GistManager className='header-block' buttonProps={GIST_BUTTON_PROPS} />
            <div className="offline-only">[connection lost, reconnecting…]</div>
            <BranchSelect tabIndex={3} useAriaLabel />
        </header>
        <div className="content">
            {codeEditor}
        </div>
    </section>;
};