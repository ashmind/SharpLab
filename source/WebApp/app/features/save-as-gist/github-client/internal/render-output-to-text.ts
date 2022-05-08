import type { RunResult } from '../../../types/results';

export default function renderOutputToText(value: RunResult['value']) {
    if (typeof value === 'string')
        return value;

    return value?.output.map(item => {
        if (typeof item === 'string')
            return item;

        switch (item.type) {
            case 'inspection:simple':
                return `[${item.title}:${item.value ?? ''}]\r\n`;
            default:
                // TODO: nicer presentation
                return JSON.stringify(item, null, 4) + '\r\n';
        }
    }).join('') ?? '';
}