import React from 'react';
import type { SimpleInspection } from '../../../shared/resultTypes';

type Props = {
    inspection: SimpleInspection;
};

const getExceptionNotice = (exception: string) => {
    if (exception.includes('System.BadImageFormatException')) {
        return <>
            <p>Note: This exception is likely caused by SharpLab itself, and not the C# compiler.</p>

            <p>Try adding <code>[assembly: SharpLab.Runtime.NoILRewriting]</code> to your code.<br />
            If exception disappears, this is definitely a SharpLab issue, and should be <a href="https://github.com/ashmind/SharpLab/issues">reported as such</a>.</p>
        </>;
    }
    return null;
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

    const exceptionNotice = isException && inspection.value && getExceptionNotice(inspection.value);

    return <div className={className}>
        <header>{inspection.title}</header>
        {!isTitleOnly && <>
            {exceptionNotice && <div className="inspection-exception-notice markdown">{exceptionNotice}</div>}
            <div className="inspection-value">{inspection.value}</div>
        </>}
    </div>;
};