import React, { FC, HTMLAttributes, useEffect, useState } from 'react';
import { classNames } from 'app/helpers/classNames';
import { useIds } from 'app/helpers/useIds';
import type { Gist } from 'ts/types/gist';
import githubAuth from 'ts/helpers/github/auth';
import { GistSaveContext, GistSaveModal } from './gist-manager/GistSaveModal';

type Props = {
    className?: string;
    gist: Gist | null;
    useLabel?: boolean;
    context: GistSaveContext;
    onSave: (gist: Gist) => void;

    buttonProps: Omit<HTMLAttributes<HTMLButtonElement>, 'id'|'onClick'>;
};
export { GistSaveContext };

// only doing it once per page load, even if
// multiple app-gist-managers are created
let postGitHubAuthRedirectModalOpened = false;

export const GistManager: FC<Props> = ({ className, gist, useLabel, context, onSave, buttonProps }) => {
    const ids = useIds(['action']);
    const [modalOpen, setModalOpen] = useState(false);

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
        buttonProps.className,
        'gist-create oline-only'
    );
    const renderOpenOrCreate = () => {
        if (gist) {
            return <a
                className="gist-link"
                id={ids.action}
                href={gist.url}
                title={`Gist: ${gist.name}`}
                target="_blank"
                rel="noopener">{useLabel ? gist.name : `Gist: ${gist.name}`}</a>;
        }

        return <button
            {...buttonProps}
            id={ids.action}
            className={buttonClassName}
            onClick={onCreateClick}>{useLabel ? 'Create' : 'Create Gist'}</button>;
    };

    const panel = <div className={className}>
        {useLabel && <label htmlFor={ids.action}>Gist:</label>}
        {renderOpenOrCreate()}
    </div>;

    const onModalSave = (gist: Gist) => {
        setModalOpen(false);
        onSave(gist);
    };

    const modal = modalOpen && <GistSaveModal
        context={context}
        onSave={onModalSave}
        onCancel={() => setModalOpen(false)} />;

    return <>
        {panel}
        {modal}
    </>;
};