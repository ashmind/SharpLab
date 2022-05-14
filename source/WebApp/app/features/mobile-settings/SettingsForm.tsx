import React, { FC } from 'react';
import { CodeEditorSwitch } from '../../features/cm6-preview/CodeEditorPreviewSwitch';
import { GistManager } from '../../features/save-as-gist/GistManager';
import { LanguageSelect } from '../../header/LanguageSelect';
import { ModeSelect } from '../../header/ModeSelect';
import { TargetSelect } from '../../header/TargetSelect';
import { useIds } from '../../helpers/useIds';
import { BranchDetailsSection } from '../roslyn-branches/BranchDetailsSection';
import { BranchSelect } from '../roslyn-branches/BranchSelect';

type Props = Record<string, never>;

export const SettingsForm: FC<Props> = () => {
    const ids = useIds(['language', 'branch', 'target', 'mode']);

    return <form className="modal-body form-aligned" onSubmit={e => e.preventDefault()}>
        <fieldset>
            <legend>Main</legend>
            <div className="form-line">
                <label htmlFor={ids.language}>Language:</label>
                <LanguageSelect id={ids.language} />
            </div>
            <div className="form-line">
                <label htmlFor={ids.branch}>Branch:</label>
                <BranchSelect id={ids.branch} />
            </div>
            <BranchDetailsSection headerless />
            <div className="form-line">
                <label htmlFor={ids.target}>Output:</label>
                <TargetSelect id={ids.target} />
            </div>
            <div className="form-line">
                <label htmlFor={ids.mode}>Build:</label>
                <ModeSelect id={ids.mode} />
            </div>
        </fieldset>

        <fieldset>
            <legend>Other</legend>
            <div className="form-line">
                <GistManager useLabel />
            </div>
            <div className="form-line">
                <CodeEditorSwitch />
            </div>
        </fieldset>
    </form>;
};