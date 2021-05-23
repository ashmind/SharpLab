import Vue from 'vue';
import type { OutputItem } from '../ts/types/results';
import OutputViewGroup from './internal/app-output-view-group';
import OutputViewSimple from './internal/app-output-view-simple';
import OutputViewMemory from './internal/app-output-view-memory';
import OutputViewGraph from './internal/app-output-view-graph';

export default Vue.component('app-output-view', {
    props: {
        output: Object as () => Array<OutputItem>|string // Array || string
    },
    computed: {
        parsedOutput(): ReadonlyArray<OutputItem> {
            if (Array.isArray(this.output))
                return this.output;
            return parseOutput(this.output);
        }
    },
    template: '#app-output-view',
    components: {
        'sub-output-view-group': OutputViewGroup,
        'sub-output-view-simple': OutputViewSimple,
        'sub-output-view-memory': OutputViewMemory,
        'sub-output-view-graph': OutputViewGraph
    }
});

type Inspection = Exclude<OutputItem, string>;
function parseOutput(output: string) {
    return output.split(/\r\n|\r|\n/g).map(line => {
        if (!line.startsWith('#{'))
            return line;

        const json = line.substr(1);
        try {
            const candidate = JSON.parse(json) as Inspection | { type?: undefined };
            return (candidate.type && candidate.type.startsWith('inspection:')) ? candidate : line;
        }
        catch {
            return line;
        }
    });
}