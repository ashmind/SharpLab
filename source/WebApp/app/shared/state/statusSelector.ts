import { selector } from 'recoil';
import { onlineState } from './onlineState';
import { resultSelector } from './resultState';

export type Status = 'default' | 'error' | 'offline';

export const statusSelector = selector<Status>({
    key: 'app-status',
    get: ({ get }) => {
        if (!get(onlineState))
            return 'offline';

        const result = get(resultSelector);
        const error = !!(result && !result.success);
        return error ? 'error' : 'default';
    }
});