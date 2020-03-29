import type { SimulationNodeDatum } from 'd3-force';

export interface DataNode {
    readonly id: number;
    readonly nestedNodes?: ReadonlyArray<DataNode>;
}

export interface StackNode extends DataNode {
    readonly offset: number;
    readonly size: number;
}

export interface ExtendedNode extends DataNode {
    isStack?: boolean;
    parentId?: number;
}

export interface DataNodeReference {
    readonly from: number;
    readonly to: number;
}

interface BaseNodeDatumData {
    node: ExtendedNode;
    element: HTMLElement;
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

export interface NodeRect {
    top: number;
    left: number;
    bottom: number;
    right: number;
    width: number;
    height: number;
}