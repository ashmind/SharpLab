import CodeMirror from 'codemirror';

import getBranchesAsync from './server/get-branches-async';

import state from './state';
import uiAsync from './ui';

let savedApplyAnnotations;
async function processChangeAsync(code, applyAnnotations) {
    this.options.branchId = this.branch ? this.branch.id : null;
    state.save(this);
    if (this.code === undefined || this.code === '')
        return [];

    const branchUrl = this.branch ? this.branch.url : null;

    //this.loading = true;

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
                { message: report }
            ]
        };
    }

    if (pendingRequest === resultPromise)
        pendingRequest = null;
    this.loading = false;
    this.updateAnnotations();
}

function applyUpdateResult(result) {
    this.result = {
        success: true,
        decompiled: result.x.decompiled,
        errors: [],
        warnings: []
    };
}

function lintCodeAsync(code, applyAnnotations) {
    savedApplyAnnotations = applyAnnotations;
    this.code = code;
    return this.processChangeAsync();
}

async function createAppAsync() {
    const app = Object.assign({
        codeMirrorModes: {
            csharp: 'text/x-csharp',
            vbnet:  'text/x-vb',
            il:     ''
        },

        branchGroups: [],
        branch: null,

        loading: false,

        result: {
            success: true,
            decompiled: '',
            errors: [],
            warnings: []
        }
    });
    state.load(app);

    const branchesPromise = (async () => {
        const branches = await getBranchesAsync();
        const groups = {};
        for (let branch of branches) {
            let group = groups[branch.group];
            if (!group) {
                group = { name: branch.group, branches: [] };
                groups[branch.group] = group;
                app.branchGroups.push(group);
            }
            group.branches.push(branch);
        }
        return branches;
    })();

    if (app.options.branchId) {
        const branches = await branchesPromise;
        app.branch = branches.filter(b => b.id === app.options.branchId)[0];
    }

    app.applyUpdateResult = applyUpdateResult.bind(app);
    return app;
}

(async function runAsync() {
    const app = await createAppAsync();
    const ui = await uiAsync(app);

    for (let name of ['options', 'branch']) {
        ui.watch(name,  () => app.processChangeAsync(), { deep: true });
    }
})();