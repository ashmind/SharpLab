import React from 'react';
import type { Diagnostic as DiagnosticItem, ServerError } from '../../shared/resultTypes';

type Props = {
    data: DiagnosticItem|ServerError;
};

export const Diagnostic: React.FC<Props> = ({ data }) => {
    return <div className="diagnostic">
        {'id' in data && `${data.severity} ${data.id}: `}{data.message}
    </div>;
};