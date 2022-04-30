import React, { FC, HTMLAttributes, useEffect, useId, useState } from 'react';
import { githubAuth } from '../../ts/helpers/github/githubAuth';
import type { Gist } from '../../ts/types/gist';
import { classNames } from '../helpers/classNames';
import { GistSaveModal } from './gist-manager/GistSaveModal';

type Props = {
    className?: string;
    gist: Gist | null;
    useLabel?: boolean;
    onSave: (gist: Gist) => void;

    buttonProps?: Omit<HTMLAttributes<HTMLButtonElement>, 'id'|'onClick'>;
};
export { Props as GistManagerProps };

// only doing it once per page load, even if
// multiple app-gist-managers are created
let postGitHubAuthRedirectModalOpened = false;

export const GistManager: FC<Props> = ({ className, gist, useLabel, onSave, buttonProps }) => {
    const actionId = useId();
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

    const panel = <div className={className}>
        {useLabel && <label htmlFor={actionId}>Gist:</label>}
        {renderOpenOrCreate()}
    </div>;

    const onModalSave = (gist: Gist) => {
        setModalOpen(false);
        onSave(gist);
    };

    const modal = modalOpen && <GistSaveModal
        onSave={onModalSave}
        onCancel={() => setModalOpen(false)} />;

    return <>
        {panel}
        {modal}
    </>;
};