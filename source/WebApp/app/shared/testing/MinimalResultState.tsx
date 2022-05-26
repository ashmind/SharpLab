import { useEffect } from 'react';
import { fromPartial } from '../../helpers/testing/fromPartial';
import { useDispatchResultUpdate } from '../state/resultState';

type Props = {
    error?: boolean;
};

export const MinimalResultState: React.FC<Props> = ({ error }) => {
    const dispatchResultUpdate = useDispatchResultUpdate();
    useEffect(() => {
        dispatchResultUpdate(error
            ? fromPartial({ type: 'serverError' })
            : fromPartial({ type: 'updateResult', updateResult: { diagnostics: [] } })
        );
    }, [dispatchResultUpdate, error]);
    return null;
};