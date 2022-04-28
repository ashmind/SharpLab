import type { ResultUpdateAction } from 'app/shared/contexts/ResultContext';
import type { ParsedResult } from 'ts/types/results';
import { convertFromUpdateResult } from './convertFromUpdateResult';

export const resultReducer = (_: ParsedResult | undefined, action: ResultUpdateAction): ParsedResult | undefined => {
    switch (action.type) {
        case 'updateResult':
        case 'cachedResult':
            return convertFromUpdateResult(action.updateResult, action.target);
        case 'serverError':
            return {
                success: false,
                errors: [{ message: action.message }],
                warnings: []
            };
    }
};