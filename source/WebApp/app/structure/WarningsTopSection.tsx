import React from 'react';
import { classNames } from '../helpers/classNames';
import { useExpander } from '../helpers/useExpander';
import type { DiagnosticWarning } from '../shared/resultTypes';
import { Diagnostic } from './results/Diagnostic';

type Props = {
    warnings: ReadonlyArray<DiagnosticWarning>;
    // Storybook/Tests only
    initialState?: {
        expanded?: boolean;
    };
};

export const WarningsTopSection: React.FC<Props> = ({ warnings, initialState }) => {
    const { expandedClassName, ExpanderButton } = useExpander({ initialExpanded: initialState?.expanded });

    if (warnings.length === 0)
        return null;

    const className = classNames('warnings top-section block-section', expandedClassName);
    return <section className={className}>
        <header>
            <ExpanderButton />
            <h1>Warnings</h1>
        </header>
        <div className="content">
            <ul>
                {warnings.map((w, i) => <li key={i.toString()}><Diagnostic data={w} /></li>)}
            </ul>
        </div>
    </section>;
};