import Vue from 'vue';
import type { MemoryInspection, MemoryInspectionLabel } from '../../ts/types/results';
import asLookup from '../../ts/helpers/as-lookup';
import '../app-select';

const specialChars = asLookup({
    '\r': '\\r',
    '\n': '\\n',
    '\t': '\\t',
    '\0': '\\0'
} as const);

type InterimLabel = MemoryInspectionLabel & {
    levelSpan?: number;
    levelSpanPlaceholder?: never;
};

interface SpanPlaceholder {
    readonly offset: number;
    readonly length: number;
    readonly nested?: never;

    levelSpan?: number;
    levelSpanPlaceholder: true;
}

interface FinalLabel {
    readonly name?: string;
    readonly offset: number;
    readonly length: number;
}

export default Vue.extend({
    props: {
        inspection: Object as () => MemoryInspection
    },
    data() {
        return ({
            mode: 'decimal' as 'decimal'|'hex'|'char'|unknown
        });
    },
    methods: {
        renderCellValue(value: number) {
            switch (this.mode) {
                case 'decimal': return value.toString().padStart(3, '0');
                case 'hex': return value.toString(16).toUpperCase().padStart(2, '0');
                case 'char': {
                    const char = String.fromCharCode(value);
                    return specialChars[char] ?? char;
                }
                default: return '??';
            }
        }
    },
    computed: {
        labelLevels() {
            const levels = [] as Array<Array<InterimLabel|SpanPlaceholder>>;
            addLabelsToLevelRecursive(levels as Array<Array<InterimLabel>>, this.inspection.labels, 0);

            applyCrossLevelSpansToLabels(levels);

            return levels.map(level => sortAndAddPaddingBetweenLabels(level, this.inspection.data.length));
        }
    },
    template: `
      <div class="inspection inspection-memory">
        <header>
          <span class="inspection-title">{{inspection.title}}</span>
          <app-select v-model="mode">
            <option value="decimal">Decimal</option>
            <option value="hex">Hex</option>
            <option value="char">Char</option>
          </app-select>
        </header>
        <table>
          <tr v-for="labels in labelLevels">
            <td v-for="label in labels"
                class="inspection-data-label"
                v-bind:colspan="label.length"
                v-bind:rowspan="label.levelSpan"
                v-bind:title="label.name">{{label.name}}</td>
          </tr>
          <tr>
            <td v-for="cell in inspection.data"
                class="inspection-data-cell"
                v-bind:class="{ 'inspection-data-zero': (cell === 0) }">{{renderCellValue(cell)}}</td>
          </tr>
        </table>
      </div>
    `.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});

function addLabelsToLevelRecursive(levels: Array<Array<InterimLabel>>, labels: ReadonlyArray<MemoryInspectionLabel>, index: number) {
    let level = levels[index] as Array<InterimLabel>|undefined;
    if (!level) {
        level = [];
        levels[index] = level;
    }
    for (const { name, offset, length, nested } of labels) {
        level.push({ name, offset, length, nested });
        if (nested && nested.length > 0)
            addLabelsToLevelRecursive(levels, nested, index + 1);
    }
}

function applyCrossLevelSpansToLabels(levels: ReadonlyArray<Array<InterimLabel|SpanPlaceholder>>) {
    for (let i = 0; i < levels.length; i++) {
        for (const label of levels[i]) {
            if (label.nested && label.nested.length > 0)
                continue;
            label.levelSpan = 1;
            for (let j = i + 1; j < levels.length; j++) {
                levels[j].push({ offset: label.offset, length: label.length, levelSpanPlaceholder: true });
                label.levelSpan += 1;
            }
        }
    }
}

function sortAndAddPaddingBetweenLabels(labels: ReadonlyArray<InterimLabel|SpanPlaceholder>, dataLength: number) {
    const results = [] as Array<FinalLabel>;
    labels = labels.slice(0).sort((a, b) => {
        if (a.offset > b.offset) return +1;
        if (a.offset < b.offset) return -1;
        return 0;
    });

    for (let i = 0; i < labels.length; i++) {
        const label = labels[i];
        if (i === 0 && label.offset > 0)
            results.push({ offset: 0, length: label.offset });

        if (!label.levelSpanPlaceholder)
            results.push(label);

        const next = (labels[i + 1] as InterimLabel|undefined) ?? { offset: dataLength };
        const offset = label.offset + label.length;
        const padding = { offset, length: next.offset - offset };
        if (padding.length > 0)
            results.push(padding);
    }
    return results;
}