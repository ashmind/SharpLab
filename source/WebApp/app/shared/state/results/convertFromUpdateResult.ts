import type { MaybeCached } from '../../../features/result-cache/types';
import type { UpdateResult, DiagnosticError, DiagnosticWarning, ParsedNonErrorResult, AstItem, Explanation, RunResult, RunResultLegacyValue, Flow } from '../../resultTypes';
import { type TargetName, TARGET_AST, TARGET_EXPLAIN, TARGET_VERIFY, TARGET_RUN, TARGET_IL } from '../../targets';
import { extractRangesFromIL } from './extractRangesFromIL';
import { parseOutput } from './parseOutput';

const collectErrorsAndWarnings = (target: TargetName, diagnostics: UpdateResult['diagnostics']) => {
    const errors = [];
    const warnings = [];
    for (const diagnostic of diagnostics) {
        switch (diagnostic.severity) {
            case 'error': errors.push(diagnostic as DiagnosticError); break;
            case 'warning': warnings.push(diagnostic as DiagnosticWarning); break;
        }
    }
    const success = (target === TARGET_AST || target === TARGET_EXPLAIN)
        || errors.length === 0;

    return { success, errors, warnings } as const;
};

const convertFromLegacyRunValue = (value: RunResultLegacyValue | null) => {
    if (!value)
        return null;

    const { output, flow: steps } = value;
    return {
        output,
        flow: { steps, areas: [] } as Flow
    };
};

export const convertFromUpdateResult = (
    { diagnostics, x, cached }: MaybeCached<UpdateResult>, target: TargetName
): MaybeCached<ParsedNonErrorResult> => {
    // if (cached)
    //    trackFeature('Result Cache');

    const rest = { ...collectErrorsAndWarnings(target, diagnostics), cached };
    switch (target) {
        case TARGET_VERIFY:
            return { type: 'verify', value: (x ?? null) as string | null, ...rest };
        case TARGET_AST:
            return { type: 'ast', value: (x ?? []) as ReadonlyArray<AstItem>, ...rest };
        case TARGET_EXPLAIN:
            return { type: 'explain', value: (x ?? []) as ReadonlyArray<Explanation>, ...rest };
        case TARGET_RUN: {
            const value = (typeof x === 'string')
                ? parseOutput(x)
                : convertFromLegacyRunValue(x as RunResultLegacyValue | null);
            return { type: 'run', value, ...rest };
        }
        case TARGET_IL: {
            const { code: value, ranges } = x
                ? extractRangesFromIL(x as string)
                : { code: x } as { code: typeof x; ranges: undefined };
            return { type: 'code', value: value ?? null, ranges, ...rest };
        }
    }

    return { type: 'code', value: x as string, ...rest };
};