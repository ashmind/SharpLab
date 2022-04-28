import React, { FC, ReactElement } from 'react';
import { useIds } from 'app/helpers/useIds';
import { TargetSelect } from 'app/header/TargetSelect';
import { ModeSelect } from 'app/header/ModeSelect';
import { CodeEditorSwitch } from 'app/footer/CodeEditorSwitch';
import { LanguageSelect } from 'app/header/LanguageSelect';
import { BranchSelect } from 'app/header/BranchSelect';
import { BranchDetailsSection } from 'app/code/BranchDetailsSection';
import { useOption } from 'app/shared/useOption';

type Props = {
    gistManager: ReactElement;
};

export const SettingsForm: FC<Props> = ({ gistManager }) => {
    const ids = useIds(['language', 'branch', 'target', 'mode']);
    const branch = useOption('branch');

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
            {branch && <BranchDetailsSection branch={branch} headerless />}
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
                {gistManager}
            </div>
            <div className="form-line">
                <CodeEditorSwitch />
            </div>
        </fieldset>
    </form>;
};