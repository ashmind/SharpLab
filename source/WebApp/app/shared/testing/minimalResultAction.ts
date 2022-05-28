import { fromPartial } from '../../helpers/testing/fromPartial';
import type { ResultUpdateAction } from '../state/results/ResultUpdateAction';

export const minimalResultAction = ({ error }: { error?: boolean }) => error
    ? fromPartial<ResultUpdateAction>({ type: 'serverError' })
    : fromPartial<ResultUpdateAction>({ type: 'updateResult', updateResult: { diagnostics: [] } });