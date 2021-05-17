const experimentKey = localStorage['sharplab.experiments.container'] as string|undefined;

export const containerRunTarget = experimentKey
    ? { 'runc': 'RunContainer' } as const
    : {} as { 'runc': never };

export const containerRunServerOptions = experimentKey ? { 'x-container-experiment': experimentKey } : {};