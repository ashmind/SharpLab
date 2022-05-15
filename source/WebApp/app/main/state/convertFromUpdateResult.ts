import parseOutput from '../../../ts/helpers/parse-output';
import type {
    DiagnosticError,
    DiagnosticWarning,
    ParsedNonErrorResult,
    AstItem,
    Explanation,
    RunResult,
    UpdateResult
} from '../../../ts/types/results';
import type { MaybeCached } from '../../features/result-cache/types';
import { TargetName, TARGET_AST, TARGET_EXPLAIN, TARGET_IL, TARGET_RUN, TARGET_VERIFY } from '../../shared/targets';
import { extractRangesFromIL } from './extractRangesFromIL';

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
                : x as Exclude<RunResult['value'], string>;
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