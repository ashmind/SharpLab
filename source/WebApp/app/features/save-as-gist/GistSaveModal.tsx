import React, { FC, FormEvent, useEffect, useId, useState } from 'react';
import { RecoilValue, useRecoilCallback } from 'recoil';
import { useAsyncCallback } from '../../helpers/useAsyncCallback';
import { Loader } from '../../shared/Loader';
import { Modal } from '../../shared/Modal';
import { codeState } from '../../shared/state/codeState';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { releaseOptionState } from '../../shared/state/releaseOptionState';
import { resultSelector } from '../../shared/state/resultState';
import { targetOptionState } from '../../shared/state/targetOptionState';
import { branchOptionState } from '../roslyn-branches/branchOptionState';
import type { Gist } from './Gist';
import { createGistAsync } from './github-client/gists';

type Props = {
    onSave: (gist: Gist) => void;
    onCancel: () => void;
};

export const GistSaveModal: FC<Props> = ({ onSave, onCancel }) => {
    const nameId = useId();
    const [name, setName] = useState('');
    const getSharedStateForSave = useRecoilCallback(({ snapshot }) => () => {
        const get = <T, >(state: RecoilValue<T>) => snapshot.getLoadable(state).getValue();
        const result = get(resultSelector);
        if (!result) throw new Error(`Cannot save gist before receiving initial result`);
        return {
            code: get(codeState),
            options: {
                language: get(languageOptionState),
                target: get(targetOptionState),
                release: get(releaseOptionState),
                branchId: get(branchOptionState)?.id ?? null
            },
            result
        };
    });
    const [save, saved, error, saving] = useAsyncCallback(
        async () => createGistAsync({ name, ...getSharedStateForSave() }),
        [name, getSharedStateForSave]
    );
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
                <label htmlFor={nameId}>Name:</label>
                <input id={nameId} onChange={e => setName(e.target.value)} type="text" required autoFocus />
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