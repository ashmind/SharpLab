import { selector } from 'recoil';
import { statusSelector } from '../../shared/state/statusSelector';
import { effectiveThemeSelector } from '../dark-mode/themeState';

export const DEFAULT_COLOR = '#4684ee';

export const colorSelector = selector({
    key: 'theme-color',
    get: ({ get }) => {
        const theme = get(effectiveThemeSelector);
        if (theme === 'dark')
            return '#2d2d30';

        const status = get(statusSelector);
        return {
            default: DEFAULT_COLOR,
            error: '#dc3912',
            offline: '#aaa'
        }[status];
    }
});