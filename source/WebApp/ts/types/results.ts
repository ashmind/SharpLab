import type { MirrorSharpDiagnostic } from './mirrorsharp';
import type { CodeRange } from './code-range';

export type DiagnosticWarning = Pick<MirrorSharpDiagnostic, 'id'|'message'> & { severity: 'warning' };
export type DiagnosticError = Pick<MirrorSharpDiagnostic, 'id'|'message'> & { severity: 'error' };
export type Diagnostic = DiagnosticWarning | DiagnosticError;

export interface ServerError {
    readonly message: string;
}

export interface AstItem {
    range?: string;
    properties?: { [key: string]: string };
    children?: ReadonlyArray<AstItem>;
}

export interface SimpleInspection {
    readonly type: 'inspection:simple';
    readonly title: 'Exception'|'Warning'|string;
    readonly value?: string;
}

export type OutputItem = string|SimpleInspection;

export interface FlowStep {
    readonly line: number;
    readonly skipped?: boolean;
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
    success: boolean;
    readonly errors: Array<DiagnosticError>|[ServerError];
    readonly warnings: Array<DiagnosticWarning>;
}

export interface CodeResult extends ResultBase {
    readonly type: 'code';
    value: string|null;
    ranges?: ReadonlyArray<{
        source: CodeRange;
        result: CodeRange;
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
    }|null;
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

export type NonErrorResult = CodeResult|AstResult|ExplainResult;
export type Result = NonErrorResult|ErrorResult;