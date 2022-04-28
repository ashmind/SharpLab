import React, { FC, FormEvent, useEffect, useState } from 'react';
import { useIds } from 'app/helpers/useIds';
import { Loader } from 'app/shared/Loader';
import { Modal } from 'app/shared/Modal';
import type { Gist } from 'ts/types/gist';
import { useAsync } from 'app/helpers/useAsync';
import { createGistAsync } from 'ts/helpers/github/gists';
import toRawOptions from 'ts/helpers/to-raw-options';
import { useOption } from 'app/shared/useOption';
import { useResult } from 'app/shared/useResult';
import { useCode } from 'app/shared/useCode';

type Props = {
    onSave: (gist: Gist) => void;
    onCancel: () => void;
};

export const GistSaveModal: FC<Props> = ({ onSave, onCancel }) => {
    const ids = useIds(['name']);
    const [name, setName] = useState('');
    const code = useCode();
    const [language, target, release, branch] = [
        useOption('language'),
        useOption('target'),
        useOption('release'),
        useOption('branch')
    ];
    const result = useResult();
    const [save, saved, error, saving] = useAsync(async () => {
        if (!result) throw new Error(`Cannot save gist before receiving initial result`);
        return await createGistAsync({
            name,
            code,
            options: toRawOptions({ language, target, release, branch }),
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            result
        });
    }, [name, code, language, target, release, branch, result]);
    const canSave = name && !saving;
    const errorMessage = error && ((error as { message?: string }).message ?? error);

    useEffect(() => {
        if (saved)
            onSave(saved);
    }, [onSave, saved]);

    const onFormSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        save();
    };

    return <Modal title="Gist" onClose={!saving ? onCancel : null}>
        <small className="disclaimer">
            <p>The main purpose is to shorten SharpLab URLs, so the functionality is minimal at the moment.</p>
            <p>For example, you can't update existing Gists.</p>
        </small>
        <form className="modal-body" onSubmit={onFormSubmit}>
            <div className="form-line">
                <label htmlFor={ids.name}>Name:</label>
                <input id={ids.name} onChange={e => setName(e.target.value)} type="text" required autoFocus />
            </div>

            <div className="form-line form-errors">{errorMessage as string}</div>

            <div className="form-line modal-buttons">
                <button
                    disabled={!canSave}
                    className="form-button-primary"
                    type="submit"
                >
                    {!saving ? 'Create Gist' : <Loader inline loading />}
                </button>
            </div>
        </form>
    </Modal>;
};