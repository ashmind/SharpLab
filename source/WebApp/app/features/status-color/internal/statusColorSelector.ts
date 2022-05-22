import { selector } from 'recoil';
import { statusSelector } from '../../../shared/state/statusSelector';
import { effectiveThemeSelector } from '../../dark-mode/themeState';

export const DEFAULT_STATUS_COLOR = '#4684ee';

export const statusColorSelector = selector({
    key: 'status-color',
    get: ({ get }) => {
        const theme = get(effectiveThemeSelector);
        if (theme === 'dark')
            return '#2d2d30';

        const status = get(statusSelector);
        return {
            default: DEFAULT_STATUS_COLOR,
            error: '#dc3912',
            offline: '#aaa'
        }[status];
    }
});