import React from 'react';

type Props = {
    children: React.ReactNode;
};

export const DarkModeRoot: React.FC<Props> = ({ children }) => {
    return <div className="theme-dark">
        {children}
    </div>;
};