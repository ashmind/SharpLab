import React, { FC, ReactElement } from 'react';
import { BranchSelect } from './header/BranchSelect';
import { LanguageSelect } from './header/LanguageSelect';

type Props = {
    codeEditor: ReactElement;
    getGistManager: (props: {
        className: string;
        buttonProps: {
            className: string;
            tabIndex: number;
        };
    }) => ReactElement;
};

export const CodeTopSection: FC<Props> = ({ codeEditor, getGistManager }) => {
    return <section className="top-section code">
        <header>
            <h1>Code</h1>
            <LanguageSelect tabIndex={1} useAriaLabel />
            {getGistManager({
                className: 'header-block',
                buttonProps: {
                    className: 'header-text-button',
                    tabIndex: 2
                }
            })}
            <div className="offline-only">[connection lost, reconnecting…]</div>
            <BranchSelect tabIndex={3} useAriaLabel />
        </header>
        <div className="content">
            {codeEditor}
        </div>
    </section>;
};