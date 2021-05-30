const experimentKey = localStorage['sharplab.experiments.container'] as string|undefined;

export const containerRunServerOptions = experimentKey ? { 'x-container-experiment': experimentKey } : {};