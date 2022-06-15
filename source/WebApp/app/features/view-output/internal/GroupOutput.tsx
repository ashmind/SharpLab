import React from 'react';
import type { SimpleInspection } from '../../../shared/resultTypes';
import { SimpleOutput } from './SimpleOutput';

// TODO: Check if server actually returns those and move to results if so
export type InspectionGroup = {
    type: 'inspection:group';
    title: string;
    limitReached?: boolean;
    inspections: ReadonlyArray<SimpleInspection|InspectionGroup>;
};

type Props = {
    group: InspectionGroup;
};

export const GroupOutput: React.FC<Props> = ({ group }) => {
    const renderInspection = (inspection: SimpleInspection|InspectionGroup, index: number) => {
        switch (inspection.type) {
            case 'inspection:group': return <GroupOutput key={index} group={inspection} />;
            case 'inspection:simple': return <SimpleOutput key={index} inspection={inspection} />;
        }
    };

    return <div className="inspection inspection-group">
        <header>{group.title}</header>
        <ol className="inspection-nested-items">
            {group.inspections.map((inspection, index) => <li>{renderInspection(inspection, index)}</li>)}
            {group.limitReached && <li className="inspection-nested-text-item">(truncated)</li>}
        </ol>
    </div>;
};