import React, { FC, HTMLAttributes, useEffect, useId, useState } from 'react';
import { useRecoilState } from 'recoil';
import { classNames } from '../../helpers/classNames';
import type { Gist } from './gist';
import { GistSaveModal } from './GistSaveModal';
import { gistState } from './gistState';
import { githubAuth } from './github-client/githubAuth';

type Props = {
    className?: string;
    useLabel?: boolean;

    buttonProps?: Omit<HTMLAttributes<HTMLButtonElement>, 'id'|'onClick'>;
};
export { Props as GistManagerProps };

// only doing it once per page load, even if
// multiple app-gist-managers are created
let postGitHubAuthRedirectModalOpened = false;

export const GistManager: FC<Props> = ({ useLabel, buttonProps }) => {
    const actionId = useId();
    const [modalOpen, setModalOpen] = useState(false);
    const [gist, setGist] = useRecoilState(gistState);

    const onCreateClick = () => {
        if (githubAuth.redirectIfRequired())
            return;
        setModalOpen(true);
    };

    useEffect(() => {
        if (githubAuth.isBackFromRedirect && !postGitHubAuthRedirectModalOpened) {
            setModalOpen(true);
            postGitHubAuthRedirectModalOpened = true;
        }
    }, []);

    const buttonClassName = classNames(
        buttonProps?.className,
        'gist-create oline-only'
    );
    const renderOpenOrCreate = () => {
        if (gist) {
            return <a
                className="gist-link"
                id={actionId}
                href={gist.url}
                title={`Gist: ${gist.name}`}
                target="_blank"
                rel="noopener">{useLabel ? gist.name : `Gist: ${gist.name}`}</a>;
        }

        return <button
            {...buttonProps}
            id={actionId}
            className={buttonClassName}
            onClick={onCreateClick}>{useLabel ? 'Create' : 'Create Gist'}</button>;
    };

    const panel = <div className="gist-manager">
        {useLabel && <label htmlFor={actionId}>Gist:</label>}
        {renderOpenOrCreate()}
    </div>;

    const onModalSave = (gist: Gist) => {
        setModalOpen(false);
        setGist(gist);
    };

    const modal = modalOpen && <GistSaveModal
        onSave={onModalSave}
        onCancel={() => setModalOpen(false)} />;

    return <>
        {panel}
        {modal}
    </>;
};