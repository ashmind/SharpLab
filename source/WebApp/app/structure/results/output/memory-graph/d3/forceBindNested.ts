import type { ExtendedNodeDatum, NestedNodeDatumData } from '../types';

export const forceBindNested = () => {
    let nodes: ReadonlyArray<ExtendedNodeDatum<NestedNodeDatumData>>;
    const force = () => {
        for (const node of nodes) {
            const { parent, dx, dy } = node.data.nested;
            node.fx = parent.x + dx;
            node.fy = parent.y + dy;
        }
    };
    force.initialize = (ns: ReadonlyArray<ExtendedNodeDatum>) => {
        nodes = ns.filter(n => n.data.nested) as ReadonlyArray<ExtendedNodeDatum<NestedNodeDatumData>>;
    };

    return force;
};