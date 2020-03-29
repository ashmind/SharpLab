import type { OutputItem } from '../../../types/results';

export default function renderOutputToText(output: ReadonlyArray<OutputItem>) {
    return output.map(item => {
        if (typeof(item) === 'string')
            return item;

        switch (item.type) {
            case 'inspection:simple':
                return `[${item.title}:${item.value}]\r\n`;
            default:
                // TODO: nicer presentation
                return JSON.stringify(item, null, 4) + '\r\n';
       }
    }).join('');
}