import Vue from 'vue';
import type { SimulationLinkDatum } from 'd3-force';
// eslint-disable-next-line no-duplicate-imports
import * as d3 from 'd3-force';
import { debounce } from 'throttle-debounce';
import ResizeObserver from 'resize-observer-polyfill';
import type { MemoryGraphInspection, MemoryGraphNode } from '../../ts/types/results';
import extendType from '../../ts/helpers/extend-type';
import type { ExtendedNode, ExtendedNodeDatum, NodeDatumData, NestedNodeDatumData, TopLevelNodeDatumData, NodeRect } from './graph-layout/interfaces';
import forceRepealNodeIntersections from './graph-layout/force-repeal-node-intersections';
import forceRepealBoundary from './graph-layout/force-repeal-boundary';
import forceBindNested from './graph-layout/force-bind-nested';


const nodeLayoutMargin = 2.4;

interface SvgLink {
    readonly key: string;
    path: string;
}

export default Vue.extend({
    props: {
        inspection: Object as () => MemoryGraphInspection
    },
    data: () => extendType({
        svgLinks: [] as ReadonlyArray<SvgLink>
    })<{
        mustResetSvgLinks?: boolean;
        lastKnownContainerRect?: DOMRect;
        resizeObserver?: ResizeObserver;
    }>(),
    computed: {
        sortedStack() {
            const nodes = this.inspection.stack.slice(0);
            nodes.sort((a, b) => {
                if (a.offset > b.offset) return 1;
                if (a.offset < b.offset) return -1;
                return 0;
            });
            const entries = [];
            let last = null;
            for (const node of nodes) {
                const separatorSize = last ? node.offset - (last.offset + last.size) : 0;
                if (separatorSize > 0)
                    entries.push({ isSeparator: true, size: separatorSize });
                entries.push(node);
                last = node;
            }
            return entries;
        }
    },
    methods: {
        resetSvgLinks() {
            this.svgLinks = this.inspection.references.map(r => ({
                key: r.from + '-' + r.to,
                path: ''
            }));
        },

        layout() {
            if (this.mustResetSvgLinks) {
                this.resetSvgLinks();
                this.mustResetSvgLinks = false;
            }

            const { stack, heap, references } = this.inspection;
            const nodes = [] as Array<ExtendedNode>;
            this.collectNodes(nodes, stack, { isStack: true });
            this.collectNodes(nodes, heap);

            const nodeElements = Array.from(this.$el.querySelectorAll('[data-app-node]')) as ReadonlyArray<HTMLElement>;
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            const nodeElementsById = Object.fromEntries(nodeElements.map(e => [e.dataset.appNode!, e])) as { [id: string]: HTMLElement };
            const svgLinksByKey = Object.fromEntries(this.svgLinks.map(l => [l.key, l]));

            const containerRect = this.$el.getBoundingClientRect();
            const heapRect = this.getOffsetClientRect(this.$refs.heap as HTMLElement, containerRect);
            const heapBoundary = {
                left: heapRect.left + 10,
                top: heapRect.top + 10
            };

            const d3NodesById = {} as { [key: string]: ExtendedNodeDatum };
            const d3Nodes = nodes.map(node => {
                const { id, isStack, parentId } = node;
                const element = nodeElementsById[id];
                const { left, top, width, height } = this.getOffsetClientRect(element, containerRect);
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
                    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                    const parentD3Node = d3NodesById[parentId]! as ExtendedNodeDatum<TopLevelNodeDatumData>;
                    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                    const siblingNodes = parentD3Node.data.node.nestedNodes!;
                    (data as NestedNodeDatumData).nested = {
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
            });

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
                        svgLink: svgLinksByKey[r.from + '-' + r.to]
                    },
                    source,
                    target
                };
            });

            (d3.forceSimulation(d3Nodes)
              .force('link',
                  d3.forceLink<ExtendedNodeDatum, SimulationLinkDatum<ExtendedNodeDatum>>()
                    .links(d3Links)
                    .strength(l => (l.source as ExtendedNodeDatum).data.node.isStack ? 5 : 2)
              )
              .force('heap-boundary', forceRepealBoundary(n => this.getNodeRect(n), heapBoundary))
              .force('intersections', forceRepealNodeIntersections(n => this.getNodeRect(n, { margin: nodeLayoutMargin })))
              .force('nested', forceBindNested())
              .tick(400) as unknown as d3.Simulation<ExtendedNodeDatum, undefined>)
              .stop();

            for (const node of d3Nodes) {
                const { element, isDomLayout, width, height } = node.data;
                if (isDomLayout)
                    continue;

                element.style.transform = `translate(${node.x - (width / 2)}px, ${node.y - (height / 2)}px)`;
            }

            for (const link of d3Links) {
                const { source, target, data: { svgLink } } = link;
                const fromRect = this.getNodeRect(source);
                const toRect = this.getNodeRect(target);
                const { nested, node: { isStack } } = source.data;

                const points = this.getConnectionPoints(fromRect, toRect, {
                    allowVertical: !isStack && (!nested || nested.isLast)
                });
                svgLink.path = this.renderSvgPath(points);
            }

            const nodeRects = d3Nodes.map(n => this.getNodeRect(n));
            const maxBottom = Math.max(...nodeRects.map(r => r.bottom));
            const maxRight = Math.max(...nodeRects.map(r => r.right));
            (this.$refs.heap as HTMLElement).style.height = maxBottom - heapRect.top + 'px';
            (this.$refs.heap as HTMLElement).style.minWidth = maxRight - heapRect.left + 'px';
            this.lastKnownContainerRect = this.$el.getBoundingClientRect();
        },

        collectNodes(result: Array<ExtendedNode>, source: ReadonlyArray<MemoryGraphNode>, extras: { isStack?: boolean; parentId?: number }|null = null) {
            for (const node of source) {
                let extended = node as ExtendedNode;
                if (extras)
                    extended = { ...node, ...extras };
                result.push(extended);

                if (node.nestedNodes) {
                    const nestedExtras = { parentId: node.id, ...extras };
                    this.collectNodes(result, node.nestedNodes, nestedExtras);
                }
            }
        },

        getNodeRect({ x, y, data: { width, height } }: ExtendedNodeDatum, { margin = 0 } = {}): NodeRect {
            return {
                top: y - (height / 2) - margin,
                left: x - (width / 2) - margin,
                bottom: y + (height / 2) + margin,
                right: x + (width / 2) + margin,
                width: width + (2 * margin),
                height: height + (2 * margin)
            };
        },

        getOffsetClientRect(element: HTMLElement, parentRect: NodeRect) {
            const { top, left, bottom, right, width, height } = element.getBoundingClientRect();
            return {
                top: top - parentRect.top,
                left: left - parentRect.left,
                bottom: bottom - parentRect.top,
                right: right - parentRect.left,
                width, height
            };
        },

        getConnectionPoints(from: NodeRect, to: NodeRect, { allowVertical }: { allowVertical: boolean }) {
            // from inside to
            if (from.top >= to.top && from.bottom <= to.bottom && from.left >= to.left && from.right <= to.right) {
                return {
                    from: { x: from.right, y: from.top + (from.height / 2) },
                    to: { x: to.left + (to.width / 2), y: to.top },
                    arc: true
                };
            }

            const horizontalOffset = to.left > from.left ? (to.left - from.left) : (from.left - to.left);
            // to below from
            if (allowVertical && to.top > from.bottom && to.top - from.bottom > horizontalOffset) {
                return {
                    from: { x: from.left + (from.width / 2), y: from.bottom },
                    to:   { x: to.left + (to.width / 2),     y: to.top }
                };
            }

            // to above from
            if (to.bottom < from.top && from.top - to.bottom > horizontalOffset) {
                return {
                    from: { x: from.right, y: from.top + (from.height / 2) },
                    to:   { x: to.left + (to.width / 2), y: to.bottom }
                };
            }

            if (to.right < from.left) {
                return {
                    from: { x: from.left, y: from.top + (from.height / 2) },
                    to:   { x: to.right,  y: to.top + (to.height / 2) }
                };
            }

            return {
                from: { x: from.right, y: from.top + (from.height / 2) },
                to:   { x: to.left,    y: to.top + (to.height / 2) }
            };
        },

        renderSvgPath(
            { from, to, arc = false }: { from: { x: number; y: number }; to: { x: number; y: number }; arc?: boolean }
        ) {
            const start = `M${from.x} ${from.y}`;
            if (arc) {
                const r = Math.max(Math.abs(to.y - from.y), Math.abs(to.x - from.x));
                return `${start} A${r} ${r} 0 1 0 ${to.x} ${to.y}`;
            }
            return `${start} L${to.x} ${to.y}`;
        },

        wasResized(rect: DOMRect, previous: DOMRect) {
            return rect.left !== previous.left
                || rect.right !== previous.right
                || rect.top !== previous.top
                || rect.bottom !== previous.bottom;
        }
    },
    mounted() {
        this.lastKnownContainerRect = this.$el.getBoundingClientRect();
        this.mustResetSvgLinks = true;
        this.layout();

        const debouncedLayout = debounce(100, () => this.layout());
        this.resizeObserver = new ResizeObserver(() => {
            const containerRect = this.$el.getBoundingClientRect();
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            if (!this.wasResized(containerRect, this.lastKnownContainerRect!))
                return;

            this.lastKnownContainerRect = containerRect;
            debouncedLayout();
        });
        this.resizeObserver.observe(this.$el);

        this.$watch('inspection', (oldValue, newValue) => {
            if (oldValue === newValue)
                return;
            this.mustResetSvgLinks = true;
            debouncedLayout();
        });
    },
    template: '#app-output-view-graph'
});