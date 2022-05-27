import React from 'react';
import { classNames } from '../helpers/classNames';
import { useExpander } from '../helpers/useExpander';
import { Loader } from '../shared/Loader';
import type { Result } from '../shared/resultTypes';
import { Diagnostic } from './results/Diagnostic';

type Props = {
    errors: Result['errors'];
};

export const ErrorsTopSection: React.FC<Props> = ({ errors }) => {
    const { expandedClassName, ExpanderButton } = useExpander({ initialExpanded: true });

    if (errors.length === 0)
        return null;

    const className = classNames('errors top-section', expandedClassName);
    return <section className={className}>
        <header>
            <ExpanderButton />
            <h1>Errors</h1>
        </header>
        <div className="content">
            <Loader />
            <ul>
                {errors.map((e, i) => <li key={i.toString()}><Diagnostic data={e} /></li>)}
            </ul>
        </div>
    </section>;
};