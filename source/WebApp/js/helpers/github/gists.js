import languages from '../languages.js';
import targets from '../targets.js';
import { token } from './auth.js';
import renderOutputToText from './internal/render-output-to-text.js';

// Zero-width space (\u200B) is invisible, but ensures that these will be sorted after other files
const sortWeight = '\u200B';
const optionsFileName = sortWeight + sortWeight + '.sharplab.json';

const extensionMap = {
    [languages.csharp]: '.cs',
    [languages.vb]:     '.vb',
    [languages.fsharp]: '.fs'
};

async function validateResponseAndParseJsonAsync(response) {
    if (!response.ok) {
        const text = await response.text();
        const error = new Error(`${response.status} ${response.statusText}\r\n${text}`);
        error.json = JSON.parse(text);
        throw error;
    }

    return response.json();
}

const readableJson = o => JSON.stringify(o, null, 4);

function prepareResultForGist(target, result) {
    switch (target) {
        case targets.csharp:
            return { suffix: '.decompiled.cs', content: result.value };
        case targets.il:
            return { suffix: '.il', content: result.value };
        case targets.asm:
            return { suffix: '.jit.asm', content: result.value };
        case targets.ast:
            return { suffix: '.ast.json', content: readableJson(result.value) };
        case targets.run:
            return { suffix: '.output.txt', content: renderOutputToText(result.value.output) };
        case targets.verify:
            return { suffix: '.verify.txt', content: result.value };
        case targets.explain:
            return { suffix: '.explained.json', content: readableJson(result.value) };
        default:
            return { suffix: '.processed.json', content: readableJson(result.value) };
    }
}

export async function getGistAsync(id) {
    const gist = await validateResponseAndParseJsonAsync(await fetch(`https://api.github.com/gists/${id}`));

    const [codeFileName, codeFile] = Object.entries(gist.files)[0];
    const name = codeFileName.replace(/\.[^.]+$/, '');
    const language = (
        Object.entries(extensionMap)
              .find(([,value]) => codeFile.filename.endsWith(value))
        || [languages.csharp]
    )[0];

    const optionsFile = gist.files[optionsFileName];
    const gistOptions = optionsFile ? JSON.parse(optionsFile.content) : {};

    const options = {
        language,
        target: gistOptions.target || language,
        release: gistOptions.mode === 'Release',
        branchId: gistOptions.branchId
    };

    return {
        id,
        name,
        url: gist.html_url,
        code: codeFile.content,
        options
    };
}

export async function createGistAsync({ name, code, options, result }) {
    if (!token)
        throw new Error("Can't save Gists without GitHub auth.");

    const extension = extensionMap[options.language];
    if (name.endsWith(extension))
        name = name.substr(0, name.length - extension.length);

    const codeFileName = name + extension;

    const gistOptions = {
        version: 1,
        target: options.target,
        mode: options.release ? 'Release' : 'Debug'
    };
    if (options.branchId)
        gistOptions.branch = options.branchId;

    const gistResult = prepareResultForGist(options.target, result);
    const resultFileName = sortWeight + name + gistResult.suffix;

    const response = await fetch('https://api.github.com/gists', {
        method: 'POST',
        body: JSON.stringify({
            public: false,
            files: {
                [codeFileName]: {
                    content: code
                },
                [resultFileName]: {
                    content: gistResult.content
                },
                [optionsFileName]: {
                    content: readableJson(gistOptions)
                }
            }
        }),
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }
    });

    const gist = await validateResponseAndParseJsonAsync(response);
    return {
        id: gist.id,
        url: gist.html_url,
        name,
        code,
        options
    };
}