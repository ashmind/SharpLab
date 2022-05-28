import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../shared/helpers/testing/recoilTestState';
import { fromPartial } from '../../shared/helpers/testing/fromPartial';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { codeState } from '../../shared/state/codeState';
import { GistManager } from './GistManager';
import type { Gist } from './gist';
import { gistState } from './gistState';

export default {
    component: GistManager
};

type TemplateProps = {
    gist?: Gist;
    hasLabel?: boolean;
};
const Template: React.FC<TemplateProps> = ({ gist, hasLabel } = {}) => {
    const recoilState = recoilTestState(
        [codeState, gist?.code ?? ''],
        [gistState, gist ?? null]
    );

    if (hasLabel) {
        return <form className="form-aligned">
            <div className="form-line">
                <label htmlFor="gist-manager">Gist:</label>
                <RecoilRoot initializeState={recoilState}>
                    <GistManager hasLabel actionId='gist-manager' />
                </RecoilRoot>
            </div>
        </form>;
    }

    return <div className="block-section">
        <header>
            <RecoilRoot initializeState={recoilState}>
                <GistManager className='header-block' buttonProps={{ className: 'header-text-button' }} />
            </RecoilRoot>
        </header>
    </div>;
};

const EXAMPLE_GIST = fromPartial<Gist>({
    name: 'Test Gist',
    code: '_'
});

export const Default = () => <Template />;
export const DarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
export const WithGist = () => <Template gist={EXAMPLE_GIST} />;
export const DarkModeWithGist = () => <DarkModeRoot><WithGist /></DarkModeRoot>;
export const HasLabel = () => <Template hasLabel />;
export const DarkModeHasLabel = () => <DarkModeRoot><HasLabel /></DarkModeRoot>;
export const HasLabelAndGist = () => <Template hasLabel gist={EXAMPLE_GIST} />;
export const DarkModeHasLabelAndGist = () => <DarkModeRoot><HasLabelAndGist /></DarkModeRoot>;