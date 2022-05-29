import React from 'react';
import type { LinkPosition } from './types';

type Props = {
    links: ReadonlyArray<LinkPosition>;
};

const renderSvgPath = ({ from, to, arc = false }: LinkPosition) => {
    const start = `M${from.x} ${from.y}`;
    if (arc) {
        const r = Math.max(Math.abs(to.y - from.y), Math.abs(to.x - from.x));
        return `${start} A${r} ${r} 0 1 0 ${to.x} ${to.y}`;
    }
    return `${start} L${to.x} ${to.y}`;
};

export const GraphReferenceLinks: React.FC<Props> = ({ links }) => {
    return <svg className="inspection-graph-reference-layer">
        <defs>
            <marker id="arrow" refX="6" refY="3"
                markerWidth="6" markerHeight="6"
                orient="auto-start-reverse"
                className="inspection-graph-reference-end-marker">
                <path d="M 0 0 L 6 3 L 0 6 z" />
            </marker>
        </defs>

        {links.map(l => <path
            key={l.key}
            className="inspection-graph-reference"
            d={renderSvgPath(l)}
            markerEnd="url(#arrow)" />)}
    </svg>;
};