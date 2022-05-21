import type { ParsedResult } from '../../../../ts/types/results';
import { convertFromUpdateResult } from './convertFromUpdateResult';
import type { ResultUpdateAction } from './ResultUpdateAction';

export const resultReducer = (action: ResultUpdateAction): ParsedResult => {
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