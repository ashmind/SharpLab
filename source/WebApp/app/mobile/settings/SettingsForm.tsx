import React, { FC, ReactNode } from 'react';
import { useIds } from 'app/helpers/useIds';
import { TargetSelect } from 'app/header/TargetSelect';
import type { AppOptions } from 'ts/types/app';
import { ModeSelect } from 'app/header/ModeSelect';
import { CodeEditorSwitch } from 'app/footer/CodeEditorSwitch';
import { LanguageSelect } from 'app/header/LanguageSelect';
import type { Branch } from 'ts/types/branch';
import { BranchSelect } from 'app/header/BranchSelect';
import { BranchDetailsSection } from 'app/source/BranchDetailsSection';

type Props = {
    options: AppOptions;
    branches: ReadonlyArray<Branch>;
    gistManager: ReactNode;
};

export const SettingsForm: FC<Props> = ({ options, branches, gistManager }) => {
    const ids = useIds(['language', 'branch', 'target', 'mode']);

    return <form className="modal-body form-aligned" onSubmit={e => e.preventDefault()}>
        <fieldset>
            <legend>Main</legend>
            <div className="form-line">
                <label htmlFor={ids.language}>Language:</label>
                <LanguageSelect
                    language={options.language}
                    onSelect={l => options.language = l}
                    htmlProps={{ id: ids.language }} />
            </div>
            <div className="form-line">
                <label htmlFor={ids.branch}>Branch:</label>
                <BranchSelect
                    allBranches={branches}
                    language={options.language}
                    branch={options.branch}
                    onSelect={b => options.branch = b}
                    htmlProps={{ id: ids.branch }} />
            </div>
            {options.branch && <BranchDetailsSection branch={options.branch} headerless />}
            <div className="form-line">
                <label htmlFor={ids.target}>Output:</label>
                <TargetSelect
                    target={options.target}
                    onSelect={t => options.target = t}
                    htmlProps={{ id: ids.target }} />
            </div>
            <div className="form-line">
                <label htmlFor={ids.mode}>Build:</label>
                <ModeSelect
                    mode={options.release ? 'release' : 'debug'}
                    onSelect={m => options.release = m === 'release'}
                    htmlProps={{ id: ids.mode }} />
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