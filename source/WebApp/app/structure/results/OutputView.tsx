import React, { FC } from 'react';
import type { OutputItem } from '../../ts/types/results';
import { SimpleOutput } from './output/SimpleOutput';
import { MemoryOutput } from './output/MemoryOutput';
import { MemoryGraphOutput } from './output/MemoryGraphOutput';
import { GroupOutput, InspectionGroup } from './output/GroupOutput';

type Props = {
    output: ReadonlyArray<OutputItem|InspectionGroup>;
};

export const OutputView: FC<Props> = ({ output }) => {
    const renderItem = (item: OutputItem|InspectionGroup, index: number) => {
        if (typeof item === 'string')
            return <pre key={index}>{item}</pre>;

        switch (item.type) {
            case 'inspection:simple': return <SimpleOutput key={index} inspection={item} />;
            case 'inspection:memory': return <MemoryOutput key={index} inspection={item} />;
            case 'inspection:memory-graph': return <MemoryGraphOutput key={index} inspection={item} />;
            case 'inspection:group': return <GroupOutput key={index} group={item} />;
        }
    };

    return <div className="output result-content">
        {!output.length && <div className="output-empty">Completed — no output.</div>}
        {output.map(renderItem)}
    </div>;
};