import React, { FC, ReactNode, useEffect } from 'react';

type Props = {
    title: string;
    onClose: () => void;
    children: ReactNode | ReadonlyArray<ReactNode>;
};

export const Modal: FC<Props> = ({ title, onClose, children }) => {
    useEffect(() => {
        const onEscape = (e: KeyboardEvent) => {
            if (e.key === 'Escape')
                onClose();
        };
        document.addEventListener('keyup', onEscape);
        return () => document.removeEventListener('keyup', onEscape);
    });

    return <div className="modal-wrapper">
        <div className="modal">
            <header>
                <span>{title}</span>
                <button type="button"
                    className="modal-close-button"
                    onClick={onClose}
                    disabled={!onClose} />
            </header>
            <div>
                {children}
            </div>
        </div>
    </div>;
};