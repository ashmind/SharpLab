import { classNames } from 'app/helpers/classNames';
import { useExpander } from 'app/helpers/useExpander';
import React, { FC } from 'react';
import type { DiagnosticWarning } from 'ts/types/results';
import { Diagnostic } from './results/Diagnostic';

type Props = {
    warnings: ReadonlyArray<DiagnosticWarning>;
};

export const WarningsTopSection: FC<Props> = ({ warnings }) => {
    const { expandedClassName, ExpanderButton } = useExpander();

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