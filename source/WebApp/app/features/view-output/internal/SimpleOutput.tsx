import React from 'react';
import type { SimpleInspection } from '../../../shared/resultTypes';

type Props = {
    inspection: SimpleInspection;
};

export const SimpleOutput: React.FC<Props> = ({ inspection }) => {
    const isMultiline = !!inspection.value && /[\r\n]/.test(inspection.value);
    const isException = inspection.title === 'Exception';
    const isWarning = inspection.title === 'Warning';
    // eslint-disable-next-line no-undefined
    const isTitleOnly = inspection.value === undefined;

    const className = [
        'inspection inspection-simple',
        isTitleOnly && 'inspection-header-only',
        isMultiline && 'inspection-multiline',
        isException && 'inspection-exception',
        isWarning && 'inspection-warning'
    ].filter(n => n).join(' ');

    return <div className={className}>
        <header>{inspection.title}</header>
        {!isTitleOnly && <div className="inspection-value">{inspection.value}</div>}
    </div>;
};