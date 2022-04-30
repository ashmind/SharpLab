// eslint-disable-next-line import/default
import type { SimulationLinkDatum } from 'd3';
import * as d3 from 'd3';
import type { MemoryGraphInspection, MemoryGraphNode } from '../../../../ts/types/results';
import { convertToD3NodesAndLinks } from './convertToD3NodesAndLinks';
import { forceBindNested } from './d3/forceBindNested';
import { forceRepealBoundary } from './d3/forceRepealBoundary';
import { forceRepealNodeIntersections } from './d3/forceRepealNodeIntersections';
import { getConnectionPoints } from './getConnectionPoints';
import { getOffsetClientRect } from './getOffsetClientRect';
import type { ExtendedNode, ExtendedNodeDatum, LayoutResult, NodeRect } from './types';

const NODE_LAYOUT_MARGIN = 2.4;

const collectNodes = (
    result: Array<ExtendedNode>,
    source: ReadonlyArray<MemoryGraphNode>,
    extras: ({ isStack?: boolean; parentId?: number } | null) = null
) => {
    for (const node of source) {
        let extended = node as ExtendedNode;
        if (extras)
            extended = { ...node, ...extras };
        result.push(extended);

        if (node.nestedNodes) {
            const nestedExtras = { parentId: node.id, ...extras };
            collectNodes(result, node.nestedNodes, nestedExtras);
        }
    }
};

const getNodeRect = ({ x, y, data: { width, height } }: ExtendedNodeDatum, { margin = 0 } = {}): NodeRect => {
    return {
        top: y - (height / 2) - margin,
        left: x - (width / 2) - margin,
        bottom: y + (height / 2) + margin,
        right: x + (width / 2) + margin,
        width: width + (2 * margin),
        height: height + (2 * margin)
    };
};

export const layout = ({ rootElement, heapElement, inspection }: {
    rootElement: HTMLElement;
    heapElement: HTMLElement;
    inspection: MemoryGraphInspection;
}): LayoutResult => {
    const { stack, heap, references } = inspection;
    const nodes = [] as Array<ExtendedNode>;
    collectNodes(nodes, stack, { isStack: true });
    collectNodes(nodes, heap);

    const containerRect = rootElement.getBoundingClientRect();
    const heapRect = getOffsetClientRect(heapElement, containerRect);
    const heapBoundary = {
        left: heapRect.left + 10,
        top: heapRect.top + 10
    };

    const { d3Nodes, d3Links } = convertToD3NodesAndLinks({
        rootElement,
        nodes,
        references,
        heapBoundary
    });

    const forceLinks = d3.forceLink<ExtendedNodeDatum, SimulationLinkDatum<ExtendedNodeDatum>>()
        .links(d3Links)
        .strength(l => (l.source as ExtendedNodeDatum).data.node.isStack ? 5 : 2);

    (d3.forceSimulation(d3Nodes)
        .force('link', forceLinks)
        .force('heap-boundary', forceRepealBoundary(n => getNodeRect(n), heapBoundary))
        .force('intersections', forceRepealNodeIntersections(n => getNodeRect(n, { margin: NODE_LAYOUT_MARGIN })))
        .force('nested', forceBindNested())
        .tick(400) as unknown as d3.Simulation<ExtendedNodeDatum, undefined>)
        .stop();

    const nodePositions = [];
    for (const node of d3Nodes) {
        const { node: { id }, isDomLayout, width, height } = node.data;
        if (isDomLayout)
            continue;

        nodePositions.push({
            id,
            x: node.x - (width / 2),
            y: node.y - (height / 2)
        });
    }

    const linkPositions = [];
    for (const link of d3Links) {
        const { source, target, data: { key } } = link;
        const fromRect = getNodeRect(source);
        const toRect = getNodeRect(target);
        const { nested, node: { isStack } } = source.data;

        const points = getConnectionPoints(fromRect, toRect, {
            allowVertical: !isStack && (!nested || nested.isLast)
        });
        linkPositions.push({ key, ...points });
    }

    const nodeRects = d3Nodes.map(n => getNodeRect(n));
    const maxBottom = Math.max(...nodeRects.map(r => r.bottom));
    const maxRight = Math.max(...nodeRects.map(r => r.right));

    return {
        heapHeight: maxBottom - heapRect.top,
        heapWidth: maxRight - heapRect.left,
        nodePositions,
        linkPositions
    };
};