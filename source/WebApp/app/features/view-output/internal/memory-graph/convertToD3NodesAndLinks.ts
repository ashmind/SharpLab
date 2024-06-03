import type { MemoryGraphReference } from '../../../../shared/resultTypes';
import { getOffsetClientRect } from './getOffsetClientRect';
import type { ExtendedNode, ExtendedNodeDatum, NodeDatumData, TopLevelNodeDatumData } from './types';

const convertToD3Node = (
    node: ExtendedNode,
    { containerRect, nodeElementsById, d3NodesById } : {
        containerRect: DOMRect;
        nodeElementsById: { readonly [id: string]: HTMLElement };
        d3NodesById: { [key: string]: ExtendedNodeDatum };
    }
) => {
    const { id, isStack, parentId } = node;
    const element = nodeElementsById[id];
    const { left, top, width, height } = getOffsetClientRect(element, containerRect);
    const x = left + (width / 2);
    const y = top + (height / 2);

    const data = {
        node,
        isDomLayout: isStack,
        element,
        width,
        height,
        topLevelLinked: []
    } as NodeDatumData;
    if (parentId) {
        const parentD3Node = d3NodesById[parentId] as ExtendedNodeDatum<TopLevelNodeDatumData>;
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const siblingNodes = parentD3Node.data.node.nestedNodes!;
        data.nested = {
            parent: parentD3Node,
            dx: x - parentD3Node.x,
            dy: y - parentD3Node.y,
            isLast: siblingNodes.map(n => n.id).indexOf(node.id) === (siblingNodes.length - 1)
        };
        data.isDomLayout = true;
    }

    const d3Node = { data, x, y } as ExtendedNodeDatum;
    if (isStack) {
        d3Node.fx = x;
        d3Node.fy = y;
    }
    d3NodesById[id] = d3Node;
    return d3Node;
};

export const convertToD3NodesAndLinks = ({ rootElement, nodes, references, heapBoundary }: {
    rootElement: HTMLElement;
    nodes: ReadonlyArray<ExtendedNode>;
    references: ReadonlyArray<MemoryGraphReference>;
    heapBoundary: { left: number; top: number };
}) => {
    const containerRect = rootElement.getBoundingClientRect();

    const nodeElements = Array.from(rootElement.querySelectorAll('[data-app-node]')) as ReadonlyArray<HTMLElement>;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const nodeElementsById = Object.fromEntries(nodeElements.map(e => [e.dataset.appNode!, e])) as { [id: string]: HTMLElement };

    const d3NodesById = {} as { [key: string]: ExtendedNodeDatum };
    const d3Nodes = nodes.map(node => convertToD3Node(node, { containerRect, nodeElementsById, d3NodesById }));

    for (const d3Node of d3Nodes) {
        const { isDomLayout, width, height } = d3Node.data;
        if (isDomLayout)
            continue;
        d3Node.x = heapBoundary.left + (width / 2);
        d3Node.y = heapBoundary.top + (height / 2);
    }

    const d3Links = references.map(r => {
        const source = d3NodesById[r.from];
        const target = d3NodesById[r.to];
        const topLevelSource = !source.data.nested ? source as ExtendedNodeDatum<TopLevelNodeDatumData> : source.data.nested.parent;
        const topLevelTarget = !target.data.nested ? target as ExtendedNodeDatum<TopLevelNodeDatumData> : target.data.nested.parent;
        topLevelSource.data.topLevelLinked.push(topLevelTarget);
        topLevelTarget.data.topLevelLinked.push(topLevelSource);
        return {
            data: {
                key: r.from + '-' + r.to
            },
            source,
            target
        } as const;
    });

    return { d3Nodes, d3Links };
};