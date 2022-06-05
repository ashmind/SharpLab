import type { MirrorSharpDiagnostic, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import type { CodeRange } from '../../app/shared/CodeRange';

export type DiagnosticWarning = Pick<MirrorSharpDiagnostic, 'id'|'message'> & { readonly severity: 'warning' };
export type DiagnosticError = Pick<MirrorSharpDiagnostic, 'id'|'message'> & { readonly severity: 'error' };
export type Diagnostic = DiagnosticWarning | DiagnosticError;

export interface ServerError {
    readonly message: string;
}

export interface AstItem {
    readonly type: 'node'|'token'|'value'|'trivia'|'operation'|string;
    readonly kind?: string;
    readonly property?: string;
    readonly value?: string | number | boolean;

    readonly range?: string;
    readonly properties?: { readonly [key: string]: string };
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
    readonly errors: ReadonlyArray<DiagnosticError>|readonly [ServerError];
    readonly warnings: ReadonlyArray<DiagnosticWarning>;
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
    } | string | null;
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
export type Result = NonErrorResult|ErrorResult;

type ParsedRunResult = Omit<RunResult, 'value'> & {
    value: Exclude<RunResult['value'], string>;
};
export type ParsedNonErrorResult = Exclude<Result, RunResult|ErrorResult>|ParsedRunResult;
export type ParsedResult = ParsedNonErrorResult|ErrorResult;

export type UpdateResult = MirrorSharpSlowUpdateResult<Result['value']>;
export type NonErrorUpdateResult = MirrorSharpSlowUpdateResult<NonErrorResult['value']>;