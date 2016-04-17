import CodeMirror from 'codemirror';

import getBranchesAsync from './server/get-branches-async.js';
import sendCodeAsync from './server/send-code-async.js';
import uiAsync from './ui/main.js';

import defaults from './state/default.js';
import urlState from './state/url.js';

let pendingRequest;
async function processCodeAsync(code) {
    this.code = code;    
    if (code === undefined || code === '')
        return [];
    
    urlState.save(this.code, this.options);
    if (pendingRequest) {
        pendingRequest.abort();
        pendingRequest = null;
    }
    
    const branchUrl = this.branch ? this.branch.url : null;
    
    this.loading = true;
    const resultPromise = sendCodeAsync(code, this.options, branchUrl);
    pendingRequest = resultPromise;

    try {
        this.result = await resultPromise;
    }
    catch (ex) {
        console.log(ex);
        const error = ex.response.data;
        let report = error.exceptionMessage || error.message;
        if (error.stackTrace)
            report += '\r\n' + error.stackTrace;

        this.result = {
            success: false,
            errors: [ report ]
        };
    }
    
    if (pendingRequest === resultPromise)
        pendingRequest = null;
    this.loading = false;
    return convertToAnnotations(
        this.result.errors,
        this.result.warnings
    );
}

function convertToAnnotations(errors, warnings) {
    const annotations = [];
    const pushAnnotations = array => {
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
    pushAnnotations(errors);
    pushAnnotations(warnings);
    return annotations;
}

function loadState() {
    const fromUrl = urlState.load() || {};
    
    let options = fromUrl.options || defaults.getOptions();
    let code = fromUrl.code || defaults.getCode(options.language);
    
    return { options, code };
}

(async function init() {
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
    await uiAsync(application);
})();