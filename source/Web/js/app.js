import CodeMirror from 'codemirror';

import getBranchesAsync from './server/get-branches-async';
import sendCodeAsync from './server/send-code-async';

import state from './state';
import uiAsync from './ui';

let pendingRequest;
let savedApplyAnnotations;
async function processChangeAsync(code, applyAnnotations) {
    this.options.branchId = this.branch ? this.branch.id : null;
    state.save(this);
    if (this.code === undefined || this.code === '')
        return [];

    if (pendingRequest) {
        pendingRequest.abort();
        pendingRequest = null;
    }

    const branchUrl = this.branch ? this.branch.url : null;

    this.loading = true;
    const resultPromise = sendCodeAsync(this.code, this.options, branchUrl);
    pendingRequest = resultPromise;

    try {
        this.result = await resultPromise;
    }
    catch (ex) {
        if (ex.reason === 'abort')
            return;

        const error = ex.response.data;
        let report = error.exceptionMessage || error.message;
        if (error.stackTrace)
            report += '\r\n' + error.stackTrace;

        this.result = {
            success: false,
            errors: [
                { message: report, start: {}, end: {}, severity: 'error' }
            ]
        };
    }

    if (pendingRequest === resultPromise)
        pendingRequest = null;
    this.loading = false;
    this.updateAnnotations();
}

function lintCodeAsync(code, applyAnnotations) {
    savedApplyAnnotations = applyAnnotations;
    this.code = code;
    return this.processChangeAsync();
}

function updateAnnotations() {
    if (!savedApplyAnnotations)
        return;

    const annotations = [];
    const push = array => {
        if (!array)
            return;

        for (let item of array) {
            annotations.push({
                severity: item.severity.toLowerCase(),
                message: item.message,
                from: CodeMirror.Pos(item.start.line, item.start.column),
                to: CodeMirror.Pos(item.end.line, item.end.column)
            });
        }
    }
    push(this.result.errors);
    push(this.result.warnings);

    savedApplyAnnotations(annotations);
}

async function createAppAsync() {
    const app = Object.assign({
        codeMirrorModes: {
            csharp: 'text/x-csharp',
            vbnet:  'text/x-vb',
            il:     ''
        },

        branches: null,
        branch: null,

        result: {
            success: true,
            decompiled: '',
            errors: [],
            warnings: []
        }
    });
    state.load(app);

    let branchesPromise = (async () => {
        app.branches = await getBranchesAsync();
    })();

    if (app.options.branchId) {
        await branchesPromise;
        app.branch = app.branches.filter(b => b.id === app.options.branchId)[0];
    }

    app.processChangeAsync = processChangeAsync.bind(app);
    app.updateAnnotations = updateAnnotations.bind(app);
    app.lintCodeAsync = lintCodeAsync.bind(app);
    return app;
}

(async function runAsync() {
    const app = await createAppAsync();
    const ui = await uiAsync(app);

    for (let name of ['options', 'branch']) {
        ui.watch(name,  () => app.processChangeAsync(), { deep: true });
    }
})();