/*
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