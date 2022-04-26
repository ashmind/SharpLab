import { classNames } from 'app/helpers/classNames';
import React, { FC } from 'react';

type Props = {
    inline?: boolean;
    loading?: boolean;
};

export const Loader: FC<Props> = ({ inline, loading }) => {
    const className = classNames('loader', loading && 'loading');
    return inline
        ? <span className={className} />
        : <div className={className} />;
};