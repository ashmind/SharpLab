import './polyfills/iterable-dom.js';

import languages from './helpers/languages.js';
import targets from './helpers/targets.js';
import getBranchesAsync from './server/get-branches-async.js';

import state from './state/index.js';
import url from './state/handlers/url.js';
import defaults from './state/handlers/defaults.js';

import uiAsync from './ui/index.js';

/* eslint-disable no-invalid-this */

function applyUpdateResult(updateResult) {
    const result = {
        success: true,
        type: this.options.target !== targets.ast ? 'code' : 'ast',
        value: updateResult.x,
        errors: [],
        warnings: []
    };
    for (const diagnostic of updateResult.diagnostics) {
        if (diagnostic.severity === 'error') {
            result.success = false;
            result.errors.push(diagnostic);
        }
        else if (diagnostic.severity === 'warning') {
            result.warnings.push(diagnostic);
        }
    }
    this.result = result;
    this.lastResultOfType[result.type] = result;
    this.loading = false;
}

function applyServerError(message) {
    this.result = {
        success: false,
        errors: [{ message }],
        warnings: []
    };
    this.loading = false;
}

function applyConnectionChange(connectionState) {
    this.online = (connectionState === 'open');
}

function getServiceUrl(branch) {
    const httpRoot = branch ? branch.url : window.location.origin;
    return `${httpRoot.replace(/^http/, 'ws')}/mirrorsharp`;
}

function applyAstHover(item) {
    if (!item || !item.range) {
        this.highlightedCodeRange = null;
        return;
    }

    const [start, end] = item.range.split(':');
    this.highlightedCodeRange = { start, end };
}

async function createAppAsync() {
    const data = Object.assign({
        languages,
        targets,

        branchGroups: [],
        branch: null,

        online: true,
        loading: true,

        result: {
            success: true,
            type: '',
            value: '',
            errors: [],
            warnings: []
        },
        lastResultOfType: { code: null, ast: null },

        highlightedCodeRange: null
    });
    await state.loadAsync(data);
    data.lastLoadedCode = data.code;

    const branchesPromise = (async () => {
        const branches = await getBranchesAsync();
        const groups = {};
        for (const branch of branches) {
            let group = groups[branch.group];
            if (!group) {
                group = { name: branch.group, branches: [] };
                groups[branch.group] = group;
                data.branchGroups.push(group);
            }
            group.branches.push(branch);
        }
        return branches;
    })();

    if (data.options.branchId) {
        const branches = await branchesPromise;
        data.branch = branches.filter(b => b.id === data.options.branchId)[0];
    }
    data.serviceUrl = getServiceUrl(data.branch);

    return {
        data,
        computed: {
            serverOptions: function() {
                return {
                    language: this.options.language,
                    optimize: this.options.release ? 'release' : 'debug',
                    'x-target': this.options.target
                };
            },
            status: function() {
                if (!this.online)
                    return { name: 'offline', color: '#aaa' };
                if (!this.result.success)
                    return { name: 'error', color: '#dc3912' };
                return { name: 'default', color: '#4684ee' };
            }
        },
        methods: { applyUpdateResult, applyServerError, applyConnectionChange, applyAstHover }
    };
}

(async function runAsync() {
    const app = await createAppAsync();
    const ui = await uiAsync(app);
    const data = app.data;

    ui.watch('options', () => state.save(data), { deep: true });
    ui.watch('code', () => state.save(data));
    ui.watch('branch', value => {
        data.options.branchId = value ? value.id : null;
        data.loading = true;
        data.serviceUrl = getServiceUrl(value);
    });

    ui.watch('options.language', (newLanguage, oldLanguage) => {
        if (data.code !== defaults.getCode(oldLanguage))
            return;
        data.code = defaults.getCode(newLanguage);
        data.lastLoadedCode = data.code;
    });

    url.changed(async () => {
        await state.loadAsync(data);
        data.lastLoadedCode = data.code;
    });
})();