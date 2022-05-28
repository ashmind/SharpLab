import React, { useEffect, useState } from 'react';
import { useRecoilState, useRecoilValue } from 'recoil';
import { classNames } from '../../shared/helpers/classNames';
import { codeState } from '../../shared/state/codeState';
import type { Gist } from './gist';
import { GistSaveModal } from './GistSaveModal';
import { gistState } from './gistState';
import { githubAuth } from './github-client/githubAuth';

type Props = {
    className?: string;
    buttonProps?: Omit<React.HTMLAttributes<HTMLButtonElement>, 'id'|'onClick'>;
} & ({
    hasLabel?: false;
    actionId?: undefined;
} | {
    hasLabel: true;
    actionId: string;
});

// only doing it once per page load, even if
// multiple app-gist-managers are created
let postGitHubAuthRedirectModalOpened = false;

export const GistManager: React.FC<Props> = ({ hasLabel, actionId, buttonProps }) => {
    const [modalOpen, setModalOpen] = useState(false);
    const code = useRecoilValue(codeState);
    const [gist, setGist] = useRecoilState(gistState);

    useEffect(() => {
        if (gist && code !== gist.code)
            setGist(null);
    }, [gist, setGist, code]);

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
        'gist-create online-only'
    );
    const renderOpenOrCreate = () => {
        if (gist) {
            return <a
                className="gist-link"
                id={actionId}
                href={gist.url}
                title={`Gist: ${gist.name}`}
                target="_blank"
                rel="noopener">{hasLabel ? gist.name : `Gist: ${gist.name}`}</a>;
        }

        return <button
            {...buttonProps}
            id={actionId}
            className={buttonClassName}
            onClick={onCreateClick}>{hasLabel ? 'Create' : 'Create Gist'}</button>;
    };

    const panel = <div className="gist-manager">
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