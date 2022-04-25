import type { SimulationNodeDatum } from 'd3-force';
import type { MemoryGraphNode } from 'ts/types/results';

export interface ExtendedNode extends MemoryGraphNode {
    isStack?: boolean;
    parentId?: number;
}

interface BaseNodeDatumData {
    node: ExtendedNode;
    width: number;
    height: number;
}

export interface TopLevelNodeDatumData extends BaseNodeDatumData {
    isDomLayout: boolean;
    nested?: never;
    topLevelLinked: Array<ExtendedNodeDatum<TopLevelNodeDatumData>>;
}

export interface NestedNodeDatumData extends BaseNodeDatumData {
    isDomLayout: true;
    nested: {
        parent: ExtendedNodeDatum<TopLevelNodeDatumData>;
        dx: number;
        dy: number;
        isLast: boolean;
    };
}

export type NodeDatumData = TopLevelNodeDatumData|NestedNodeDatumData;

export interface ExtendedNodeDatum<TNodeDatumData = NodeDatumData> extends SimulationNodeDatum {
    x: number;
    y: number;
    data: TNodeDatumData;
}

export type NodeRect = {
    readonly top: number;
    readonly left: number;
    readonly bottom: number;
    readonly right: number;
    readonly width: number;
    readonly height: number;
};

type Point = {
    readonly x: number;
    readonly y: number;
};

export type NodePosition = {
    readonly id: number;
} & Point;

export type LinkPosition = {
    readonly key: string;
    readonly from: Point;
    readonly to: Point;
    readonly arc?: boolean;
};

export type LayoutResult = {
    readonly heapHeight: number;
    readonly heapWidth: number;
    readonly nodePositions: ReadonlyArray<NodePosition>;
    readonly linkPositions: ReadonlyArray<LinkPosition>;
};