import React, { FC, HTMLAttributes, useState } from 'react';
import { Modal } from '../shared/Modal';
import { SettingsForm } from './settings/SettingsForm';

type Props = {
    buttonProps: Omit<HTMLAttributes<HTMLButtonElement>, 'className'|'onClick'|'aria-label'>;
};

export const MobileSettings: FC<Props> = ({ buttonProps }) => {
    const [modalOpen, setModalOpen] = useState(false);

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