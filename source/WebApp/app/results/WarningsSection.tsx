import { classNames } from 'app/helpers/classNames';
import { useExpander } from 'app/helpers/useExpander';
import React, { FC } from 'react';
import type { DiagnosticWarning } from 'ts/types/results';
import { Diagnostic } from './diagnostics/Diagnostic';

type Props = {
    className: string;
    warnings: ReadonlyArray<DiagnosticWarning>;
};

export const WarningsSection: FC<Props> = ({ className, warnings }) => {
    const { expandedClassName, ExpanderButton } = useExpander();

    if (warnings.length === 0)
        return null;

    const fullClassName = classNames(
        className,
        'warnings block-section',
        expandedClassName
    );
    return <section className={fullClassName}>
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