import CodeMirror from 'codemirror';

import getBranchesAsync from './server/get-branches-async';
import sendCodeAsync from './server/send-code-async';
import uiAsync from './ui';

import defaults from './state/default';
import urlState from './state/url';

let pendingRequest;
let savedApplyAnnotations;
async function processCodeAsync(code, applyAnnotations) {
    saveState(this.code, this.options);
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
    return this.processCodeAsync();
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

function loadState() {
    const fromUrl = urlState.load() || {};
    
    const options = fromUrl.options || {};
    const defaultOptions = defaults.getOptions();
    for (let key of Object.keys(defaultOptions)) {
        if (options[key] == null)
            options[key] = defaultOptions[key];
    }
    const code = fromUrl.code || defaults.getCode(options.language);
    
    return { options, code };
}

function saveState(code, options) {
    urlState.save(code, options);
}

async function createApplicationAsync() {
    const application = Object.assign({  
        codeMirrorModes: {
            csharp: 'text/x-csharp',
            vbnet:  'text/x-vb',
            il:     ''
        },

        branches: (await getBranchesAsync()),
        branch: null,
        
        result: {
            success: true,
            decompiled: '',
            errors: [],
            warnings: []
        }
    }, loadState());
 
    application.processCodeAsync = processCodeAsync.bind(application);
    application.updateAnnotations = updateAnnotations.bind(application);
    application.lintCodeAsync = lintCodeAsync.bind(application);
    return application;
}

(async function runAsync() {
    const application = await createApplicationAsync();
    const ui = await uiAsync(application);
    
    ui.watch('options', () => {
        saveState(application.code, application.options);
        application.processCodeAsync();
    }, { deep: true });
})();