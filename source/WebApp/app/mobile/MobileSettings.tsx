import React, { FC, HTMLAttributes, ReactElement, useState } from 'react';
import type { AppOptions } from 'ts/types/app';
import type { Branch } from 'ts/types/branch';
import { Modal } from '../shared/Modal';
import { SettingsForm } from './settings/SettingsForm';

type Props = {
    options: AppOptions;
    branches: ReadonlyArray<Branch>;
    // TEMP: Vue compatibility
    'button-props': HTMLAttributes<HTMLButtonElement>;
    children: ReactElement;
};

export const MobileSettings: FC<Props> = ({ options, branches, ['button-props']: buttonProps, children: gistManager }) => {
    const [modalOpen, setModalOpen] = useState(false);

    const button = <button
        className="mobile-settings-button"
        onClick={() => setModalOpen(true)}
        aria-label="Settings"
        {...buttonProps}></button>;

    const modal = modalOpen && <Modal title="Settings" onClose={() => setModalOpen(false)}>
        <SettingsForm options={options} branches={branches} gistManager={gistManager} />
    </Modal>;

    return <>
        {button}
        {modal}
    </>;
};