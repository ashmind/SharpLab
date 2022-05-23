import React, { FC, useCallback, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { debounce } from 'throttle-debounce';
import type { MemoryGraphHeapNode, MemoryGraphInspection } from 'ts/types/results';
import { GraphNodeInnerFragment } from './memory-graph/GraphNodeInnerFragment';
import { GraphReferenceLinks } from './memory-graph/GraphReferenceLinks';
import { layout as generateLayout } from './memory-graph/layout';
import { SortedStackNode, sortStack } from './memory-graph/sortStack';
import type { LayoutResult } from './memory-graph/types';

type Props = {
    inspection: MemoryGraphInspection;
};

type EmptyLayoutResult = {
    readonly heapHeight?: undefined;
    readonly heapWidth?: undefined;
    readonly nodePositions: [];
    readonly linkPositions: [];
};

const wasResized = (rect: DOMRect, previous: DOMRect) => {
    return rect.left !== previous.left
        || rect.right !== previous.right
        || rect.top !== previous.top
        || rect.bottom !== previous.bottom;
};

export const MemoryGraphOutput: FC<Props> = ({ inspection }) => {
    const rootElementRef = useRef<HTMLDivElement>(null);
    const heapElementRef = useRef<HTMLDivElement>(null);
    const [layout, setLayout] = useState<LayoutResult | EmptyLayoutResult>({
        nodePositions: [],
        linkPositions: []
    });
    const sortedStack = useMemo(() => sortStack(inspection.stack), [inspection]);

    const updateLayout = useCallback(() => {
        const rootElement = rootElementRef.current;
        const heapElement = heapElementRef.current;
        if (!rootElement || !heapElement)
            return;

        const layout = generateLayout({ rootElement, heapElement, inspection });
        setLayout(layout);
    }, [inspection]);

    useLayoutEffect(() => {
        updateLayout();
        // TODO: Likely incorrect if ref is not ready at some point during initial load
        const rootElement = rootElementRef.current;
        if (!rootElement)
            return;

        const debouncedUpdateLayout = debounce(100, () => updateLayout());
        let lastKnownContainerRect = rootElement.getBoundingClientRect();
        const resizeObserver = new ResizeObserver(() => {
            const rootElement = rootElementRef.current;
            if (!rootElement)
                return;

            const containerRect = rootElement.getBoundingClientRect();
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            if (!wasResized(containerRect, lastKnownContainerRect))
                return;

            lastKnownContainerRect = containerRect;
            debouncedUpdateLayout();
        });
        resizeObserver.observe(rootElement);

        return () => {
            resizeObserver.unobserve(rootElement);
            resizeObserver.disconnect();
        };
    }, [updateLayout]);

    const renderStackNode = (node: SortedStackNode, index: number) => {
        if (node.isSeparator)
            return <li key={'separator-' + index} className="inspection-nested-text-item">{node.size} bytes (other)</li>;

        return <li key={node.id} className="inspection-graph-node inspection-graph-node-top-level" data-app-node={node.id}>
            <GraphNodeInnerFragment node={node} />
        </li>;
    };

    const renderHeapNode = (node: MemoryGraphHeapNode) => {
        const className = 'inspection-graph-node inspection-graph-node-top-level'
            + (node.nestedNodes ? ' inspection-multiline' : '');
        const position = layout.nodePositions.find(p => p.id === node.id);
        // eslint-disable-next-line no-undefined
        const style = position ? { transform: `translate(${position.x}px, ${position.y}px)` } : undefined;

        return <div key={node.id} className={className} data-app-node={node.id} style={style}>
            <GraphNodeInnerFragment node={node} />
        </div>;
    };

    const stackSection = <section className="inspection-graph-stack">
        <header>Stack</header>
        <ol className="inspection-graph-nodes">{sortedStack.map(renderStackNode)}</ol>
    </section>;

    const heapStyle = layout.heapWidth ? {
        height: layout.heapHeight,
        minWidth: layout.heapWidth
    // eslint-disable-next-line no-undefined
    } : undefined;
    const heapSection = <section className="inspection-graph-heap" ref={heapElementRef} style={heapStyle}>
        <header>Heap</header>
        <div className="inspection-graph-nodes" ref="heap">{inspection.heap.map(renderHeapNode)}</div>
    </section>;

    return <div className="inspection inspection-graph" ref={rootElementRef}>
        <GraphReferenceLinks links={layout.linkPositions} />
        {stackSection}
        {heapSection}
    </div>;
};