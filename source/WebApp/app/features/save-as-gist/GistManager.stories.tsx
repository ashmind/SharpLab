import React from 'react';
import { fromPartial } from '../../shared/helpers/testing/fromPartial';
import { codeState } from '../../shared/state/codeState';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import { GistManager } from './GistManager';
import type { Gist } from './Gist';
import { gistState } from './gistState';

export default {
    component: GistManager
};

type TemplateProps = {
    gist?: Gist;
    hasLabel?: boolean;
};
const Template: React.FC<TemplateProps> = ({ gist, hasLabel } = {}) => {
    const render = () => {
        if (hasLabel) {
            return <form className="form-aligned">
                <div className="form-line">
                    <label htmlFor="gist-manager">Gist:</label>
                    <GistManager hasLabel actionId='gist-manager' />
                </div>
            </form>;
        }

        return <div className="block-section">
            <header>
                <GistManager className='header-block' buttonProps={{ className: 'header-text-button' }} />
            </header>
        </div>;
    };

    return <>
        <TestSetRecoilState state={codeState} value={gist?.code ?? ''} />
        <TestSetRecoilState state={gistState} value={gist ?? null} />
        <TestWaitForRecoilStates states={[codeState, gistState]}>
            {render()}
        </TestWaitForRecoilStates>
    </>;
};

const EXAMPLE_GIST = fromPartial<Gist>({
    name: 'Test Gist',
    code: '_',
    options: {}
});

export const Default = () => <Template />;
export const DarkMode = darkModeStory(Default);
export const WithGist = () => <Template gist={EXAMPLE_GIST} />;
export const DarkModeWithGist = darkModeStory(WithGist);
export const HasLabel = () => <Template hasLabel />;
export const DarkModeHasLabel = darkModeStory(HasLabel);
export const HasLabelAndGist = () => <Template hasLabel gist={EXAMPLE_GIST} />;
export const DarkModeHasLabelAndGist = darkModeStory(HasLabelAndGist);