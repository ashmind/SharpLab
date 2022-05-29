import React from 'react';

type Props = {
    message: string;
};

export const VerifyView: React.FC<Props> = ({ message }) => {
    return <div className="result-content">{message}</div>;
};