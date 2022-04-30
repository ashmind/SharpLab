import React, { FC, ReactElement } from 'react';
import { BranchDetailsSection } from '../../code/BranchDetailsSection';
import { CodeEditorSwitch } from '../../footer/CodeEditorSwitch';
import { BranchSelect } from '../../header/BranchSelect';
import { LanguageSelect } from '../../header/LanguageSelect';
import { ModeSelect } from '../../header/ModeSelect';
import { TargetSelect } from '../../header/TargetSelect';
import { useIds } from '../../helpers/useIds';
import { useOption } from '../../shared/useOption';

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