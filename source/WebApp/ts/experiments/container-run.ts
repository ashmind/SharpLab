import defineState from '../helpers/define-state';
import type { RunResult } from '../types/results';

let seed = localStorage['sharplab.experiments.container.seed'];
if (!seed) {
    seed = Math.floor(Math.random() * 10) + 1;
    localStorage['sharplab.experiments.container.seed'] = seed;
}

export const containerRunServerOptions = { 'x-container-experiment-seed': seed };

const [containerRunException, setContainerRunException] = defineState<string|null>(null);
const [containerRunActive, setContainerRunActive] = defineState<boolean>(false);

export { containerRunActive, containerRunException };

export type ContainerExperimentFallbackRunValue = {
    readonly containerExperimentException?: string
};

export function updateContainerExperimentStateFromRunResult(result: RunResult) {
    if (typeof result.value === 'string') {
        setContainerRunActive(true);
        return;
    }

    const experimentException = result.value?.containerExperimentException;
    if (experimentException)
        setContainerRunException(experimentException);
}