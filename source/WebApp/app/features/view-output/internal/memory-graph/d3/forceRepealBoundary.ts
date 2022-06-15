import type { ExtendedNodeDatum, NodeRect } from '../types';

export const forceRepealBoundary = (
    getNodeRect: (node: ExtendedNodeDatum) => NodeRect,
    { left, top }: { left: number; top: number }
) => {
    let nodes: ReadonlyArray<ExtendedNodeDatum>;
    const force = () => {
        for (const node of nodes) {
            const rect = getNodeRect(node);

            if (rect.left < left)
                node.x += left - rect.left;

            if (rect.top < top)
                node.y += top - rect.top;
        }
    };
    force.initialize = (ns: ReadonlyArray<ExtendedNodeDatum>) => {
        nodes = ns.filter(n => !n.data.isDomLayout);
    };
    return force;
};