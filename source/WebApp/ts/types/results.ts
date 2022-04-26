import type { MirrorSharpDiagnostic, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import type { ContainerExperimentFallbackRunValue } from '../experiments/container-run';
import type { CodeRange } from './code-range';

export type DiagnosticWarning = Pick<MirrorSharpDiagnostic, 'id'|'message'> & { severity: 'warning' };
export type DiagnosticError = Pick<MirrorSharpDiagnostic, 'id'|'message'> & { severity: 'error' };
export type Diagnostic = DiagnosticWarning | DiagnosticError;

export interface ServerError {
    readonly message: string;
}

export interface AstItem {
    readonly type: 'node'|'token'|'value'|'trivia'|'operation'|string;
    readonly kind?: string;
    readonly property?: string;
    readonly value?: string;

    readonly range?: string;
    readonly properties?: { [key: string]: string };
    readonly children?: ReadonlyArray<AstItem>;
}

export interface SimpleInspection {
    readonly type: 'inspection:simple';
    readonly title: 'Exception'|'Warning'|string;
    readonly value?: string;
}

export interface MemoryInspectionLabel {
    readonly name: string;
    readonly offset: number;
    readonly length: number;
    readonly nested?: ReadonlyArray<MemoryInspectionLabel>;
}

export interface MemoryInspection {
    readonly type: 'inspection:memory';
    readonly title: string;
    readonly labels: ReadonlyArray<MemoryInspectionLabel>;
    readonly data: ReadonlyArray<number>;
}

export interface MemoryGraphNode {
    readonly id: number;
    readonly value: string;
    readonly nestedNodes?: ReadonlyArray<MemoryGraphNestedNode>;
    readonly nestedNodesLimit?: true;
}

export interface MemoryGraphStackNode extends MemoryGraphNode {
    readonly title?: string;
    readonly offset: number;
    readonly size: number;
}

export interface MemoryGraphHeapNode extends MemoryGraphNode {
    readonly title: string;
}

export interface MemoryGraphNestedNode extends MemoryGraphNode {
    readonly title: string;
    readonly nestedNodes?: ReadonlyArray<MemoryGraphHeapNode>;
}

export interface MemoryGraphReference {
    readonly from: number;
    readonly to: number;
}

export interface MemoryGraphInspection {
    readonly type: 'inspection:memory-graph';
    readonly stack: ReadonlyArray<MemoryGraphStackNode>;
    readonly heap: ReadonlyArray<MemoryGraphHeapNode>;
    readonly references: ReadonlyArray<MemoryGraphReference>;
}

export type OutputItem = string|SimpleInspection|MemoryInspection|MemoryGraphInspection;

export type OutputJsonLineFlow = {
    readonly flow: ReadonlyArray<
        number
            | [line: number, value: string, name?: string]
            | { exception: string }
    >;
};

export interface FlowStep {
    readonly line: number;
    readonly skipped?: true;
    readonly notes?: string;
    readonly exception?: string;
}

export interface Explanation {
    readonly code: string;
    readonly name: string;
    readonly text: string;
    readonly link: string;
}

interface ResultBase {
    readonly success: boolean;
    readonly errors: Array<DiagnosticError>|[ServerError];
    readonly warnings: Array<DiagnosticWarning>;
}

export interface CodeResult extends ResultBase {
    readonly type: 'code';
    readonly value: string|null;
    readonly ranges?: ReadonlyArray<{
        readonly source: CodeRange;
        readonly result: CodeRange;
    }>;
}

export interface AstResult extends ResultBase {
    readonly type: 'ast';
    readonly value: ReadonlyArray<AstItem>;
}

export interface RunResult extends ResultBase {
    readonly type: 'run';
    readonly value: {
        readonly output: ReadonlyArray<OutputItem>;
        readonly flow: ReadonlyArray<FlowStep>;
    } & ContainerExperimentFallbackRunValue | string | null;
}

export interface VerifyResult extends ResultBase {
    readonly type: 'verify';
    readonly value: string|null;
}

export interface ExplainResult extends ResultBase {
    readonly type: 'explain';
    readonly value: ReadonlyArray<Explanation>;
}

export interface ErrorResult extends ResultBase {
    readonly success: false;
    readonly type?: undefined;
    readonly value?: undefined;
}

export type NonErrorResult = CodeResult|AstResult|ExplainResult|VerifyResult|RunResult;
export type Result = MaybeCached<NonErrorResult|ErrorResult>;

type ParsedRunResult = MaybeCached<Omit<RunResult, 'value'> & {
    value: Exclude<RunResult['value'], string>;
}>;
export type ParsedResult = Exclude<Result, RunResult>|ParsedRunResult;

export type MaybeCached<T> = T & {
    cached?: { date: Date };
};

export type CachedUpdateResult = MirrorSharpSlowUpdateResult<Result['value']> & {
    cached: { date: Date };
};