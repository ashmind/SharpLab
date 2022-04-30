/*
const setResult = (app: Pick<AppData, 'result' | 'lastResultOfType' | 'loadingDelay' | 'loading'>, result: Result) => {
    app.result = result;
    if (result.success) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        app.lastResultOfType[result.type] = result as any;
    }
    resetLoading(app);
};

const setResultFromUpdate = (
    app: Pick<AppData, 'result' | 'lastResultOfType' | 'firstResultWasCached' | 'loadingDelay' | 'loading'>,
    options: Pick<AppOptions, 'target'>,
    updateResult: MirrorSharpSlowUpdateResult<Result['value']> & {
        cached?: { date: Date };
    }
) => {
    const result = {
        success: true,
        type: getResultType(options.target),
        value: updateResult.x,
        errors: [],
        warnings: [],
        cached: updateResult.cached
    } as PartiallyMutable<NonErrorResult, 'success'>;
    for (const diagnostic of updateResult.diagnostics) {
        if (diagnostic.severity === 'error') {
            if (result.type !== 'ast' && result.type !== 'explain')
                result.success = false;
            result.errors.push(diagnostic as DiagnosticError);
        }
        else if (diagnostic.severity === 'warning') {
            result.warnings.push(diagnostic as DiagnosticWarning);
        }
    }
    if (options.target === targets.il && result.value) {
        const ilResult = result as PartiallyMutable<CodeResult & { value: string }, 'ranges'|'value'>;
        const { code, ranges } = extractRangesFromIL(ilResult.value);
        ilResult.value = code;
        ilResult.ranges = ranges;
    }
    if (result.type === 'run') {
        updateContainerExperimentStateFromRunResult(result as RunResult);
        if (typeof result.value === 'string')
            partiallyMutable(result)<'value'>().value = parseOutput(result.value);
    }

    if (updateResult.cached) {
        app.firstResultWasCached = true;
        trackFeature('Result Cache');
    }
    setResult(app, result as NonErrorResult);
};

function applyGistSave(this: App, gist: Gist) {
    saveStateToUrl(gist.code, gist.options, { gist });
    this.gist = gist;
}

async function createAppAsync() {
    const data = {
        languages,
        targets,

        branches: [],

        online: true,
        loading: true,

        result: {
            success: true,
            type: 'code',
            value: '',
            errors: [],
            warnings: []
        },
        lastResultOfType: { run: null, code: null, ast: null },
        firstResultWasCached: false,

        highlightedCodeRange: null,
        gist: null
    } as Omit<AppData, 'code'|'options'|'serviceUrl'> & Partial<Pick<AppData, 'code'|'options'|'serviceUrl'>>;

    // not awaiting as we don't want this to block UI
    // eslint-disable-next-line @typescript-eslint/no-floating-promises
    (async () => data.branches = await branchesPromise)();

    await loadStateAsync(data, {
        resolveBranchAsync,
        setResultFromCache: (result, options) => setResultFromUpdate(data, options, result)
    });
    data.lastLoadedCode = data.code;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    data.serviceUrl = getServiceUrl(data.options!.branch);

    return {
        data,
        computed: {
            serverOptions(this: App): ServerOptions {
                const { branch } = this.options;
                const noCache = this.firstResultWasCached
                    && (!branch || branch.sharplab?.supportsUnknownOptions);

                return {
                    'x-optimize': this.options.release ? 'release' : 'debug',
                    'x-target': this.options.target,
                    ...(noCache ? { 'x-no-cache': true } : {})
                };
            },
            status(this: App): AppStatus {
                const error = !!(this.result && !this.result.success);
                return {
                    online: this.online,
                    error,
                    color:  this.online ? (!error ? '#4684ee' : '#dc3912') : '#aaa'
                };
            }
        },
        methods: {
            applyUpdateWait,
            applyUpdateResult,
            applyServerError,
            applyConnectionChange,
            applyCodeViewRange,
            applyAstSelect,
            applyCursorMove,
            applyGistSave
        }
    } as AppDefinition;
}

// eslint-disable-next-line @typescript-eslint/no-floating-promises
(async function runAsync() {
    const app = await createAppAsync();
    const ui = await uiAsync(app);
    const data = app.data;

    ui.watch('options', () => saveState(data), { deep: true });
    ui.watch('code', () => saveState(data));
    ui.watch('options.branch', value => {
        if (value)
            trackFeature('Branch: ' + value.id);
        data.loading = true;
        data.serviceUrl = getServiceUrl(value);
    });

    ui.watch('options.language', (newLanguage, oldLanguage) => {
        trackFeature('Language: ' + newLanguage);
        const { options } = data;
        if (options.branch && newLanguage === languages.fsharp) {
            if (options.branch.kind === 'roslyn' || options.branch.id === 'core-x64-profiled')
                options.branch = null;
        }

        const target = data.options.target;
        if (data.code !== defaults.getCode(oldLanguage, target))
            return;
        data.code = defaults.getCode(newLanguage, target);
        data.lastLoadedCode = data.code;
    });

    ui.watch('options.target', (newTarget, oldTarget) => {
        trackFeature('Target: ' + newTarget);
        const language = data.options.language;
        if (data.code !== defaults.getCode(language, oldTarget))
            return;
        data.code = defaults.getCode(language, newTarget);
        data.lastLoadedCode = data.code;
    });

    // eslint-disable-next-line @typescript-eslint/no-misused-promises
    subscribeToUrlStateChanged(async () => {
        await loadStateAsync(data, {
            resolveBranchAsync,
            setResultFromCache: (result, options) => setResultFromUpdate(data, options, result)
        });
        data.lastLoadedCode = data.code;
    });
})();*/