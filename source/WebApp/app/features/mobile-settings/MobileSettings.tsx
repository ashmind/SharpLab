import React, { FC, HTMLAttributes, useState } from 'react';
import { Modal } from '../../shared/Modal';
import { SettingsForm } from './SettingsForm';

type Props = {
    buttonProps: Omit<HTMLAttributes<HTMLButtonElement>, 'className'|'onClick'|'aria-label'>;
    // Storybook/Tests only
    initialState?: {
        modalOpen?: boolean;
    };
};

export const MobileSettings: FC<Props> = ({ buttonProps, initialState }) => {
    const [modalOpen, setModalOpen] = useState(!!initialState?.modalOpen);

    const button = <button
        className="mobile-settings-button"
        onClick={() => setModalOpen(true)}
        aria-label="Settings"
        {...buttonProps}></button>;

    const modal = modalOpen && <Modal title="Settings" onClose={() => setModalOpen(false)}>
        <SettingsForm />
    </Modal>;

    return <>
        {button}
        {modal}
    </>;
};