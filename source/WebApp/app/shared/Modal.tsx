import React, { useEffect } from 'react';
import ReactDOM from 'react-dom';

// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
const modalRoot = document.getElementById('app-modals')!;

type Props = {
    title: string;
    onClose?: (() => void) | null;
    children: React.ReactNode;
};

export const Modal: React.FC<Props> = ({ title, onClose, children }) => {
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

    const modal = <div className="modal-wrapper">
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

    return ReactDOM.createPortal(
        modal,
        modalRoot
    );
};