import React from 'react';
import { classNames } from '../shared/helpers/classNames';

type Props = {
    inline?: boolean;
    loading?: boolean;
};

export const Loader: React.FC<Props> = ({ inline, loading }) => {
    const className = classNames('loader', loading && 'loading');
    return inline
        ? <span className={className} />
        : <div className={className} />;
};