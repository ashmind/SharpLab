import React, { FC } from 'react';

type Props = {
    message: string;
};

export const VerifyView: FC<Props> = ({ message }) => {
    return <div className="result-content">{message}</div>;
};