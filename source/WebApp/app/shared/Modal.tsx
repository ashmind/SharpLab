import React, { FC, ReactNode, useEffect } from 'react';

type Props = {
    title: string;
    onClose?: (() => void) | null;
    children: ReactNode | ReadonlyArray<ReactNode>;
};

export const Modal: FC<Props> = ({ title, onClose, children }) => {
    useEffect(() => {
        if (!onClose)
            return;
        const onEscape = (e: KeyboardEvent) => {
            if (e.key === 'Escape')
                onClose();
        };
        document.addEventListener('keyup', onEscape);
        return () => document.removeEventListener('keyup', onEscape);
    }, [onClose]);

    return <div className="modal-wrapper">
        <div className="modal">
            <header>
                <span>{title}</span>
                <button type="button"
                    className="modal-close-button"
                    // eslint-disable-next-line no-undefined
                    onClick={onClose ?? undefined}
                    disabled={!onClose} />
            </header>
            <div>
                {children}
            </div>
        </div>
    </div>;
};