import React, { FC } from 'react';
import type { MemoryGraphHeapNode, MemoryGraphStackNode } from 'ts/types/results';

type Props = {
    node: MemoryGraphStackNode | MemoryGraphHeapNode;
};

export const GraphNodeInnerFragment: FC<Props> = ({ node }) => {
    const renderContent = () => {
        if (!node.nestedNodes)
            return <div className="inspection-value">{node.value}</div>;

        return <ol className="inspection-nested-items">
            {node.nestedNodes.map(nested => <li className="inspection-graph-node" data-app-node={nested.id}>
                <header>{nested.title}</header>
                <div className="inspection-value">{nested.value}</div>
            </li>)}
            {node.nestedNodesLimit && <li className="inspection-nested-text-item">(truncated)</li>}
        </ol>;
    };

    return <>
        {node.title && <header>{node.title}</header>}
        {renderContent()}
    </>;
};