import React from 'react';

type Props = {
    message: string;
};

export const VerifyView: React.FC<Props> = ({ message }) => {
    return <div class="result-content">{message}</div>;
};