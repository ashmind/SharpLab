import React, { FC, FormEvent, useEffect, useState } from 'react';
import { useIds } from 'app/helpers/useIds';
import { Loader } from 'app/shared/Loader';
import { Modal } from 'app/shared/Modal';
import type { Gist } from 'ts/types/gist';
import { useAsync } from 'app/helpers/useAsync';
import { createGistAsync } from 'ts/helpers/github/gists';
import type { AppOptions } from 'ts/types/app';
import toRawOptions from 'ts/helpers/to-raw-options';
import type { Result } from 'ts/types/results';

export type GistSaveContext = {
    readonly code: string;
    readonly options: AppOptions;
    readonly result: Result;
};

type Props = {
    context: GistSaveContext;
    onSave: (gist: Gist) => void;
    onCancel: () => void;
};

export const GistSaveModal: FC<Props> = ({ context, onSave, onCancel }) => {
    const ids = useIds(['name']);
    const [name, setName] = useState('');
    const [save, saved, error, saving] = useAsync(async () => {
        return await createGistAsync({
            name,
            code: context.code,
            options: toRawOptions(context.options),
            result: context.result
        });
    }, [name, context]);
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