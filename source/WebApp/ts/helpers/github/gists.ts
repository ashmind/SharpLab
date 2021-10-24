import type { CodeResult, AstResult, RunResult, VerifyResult, ExplainResult, ErrorResult, Result } from '../../types/results';
import type { Gist } from '../../types/gist';
import extendType from '../extend-type';
import { languages, LanguageName } from '../languages';
import { TargetName, targets } from '../targets';
import asLookup from '../as-lookup';
import { token } from './auth';
import renderOutputToText from './internal/render-output-to-text';

type TargetResultMap = {
    [targets.csharp]: CodeResult;
    [targets.il]: CodeResult;
    [targets.asm]: CodeResult;
    [targets.ast]: AstResult;
    [targets.run]: RunResult;
    [targets.verify]: VerifyResult;
    [targets.explain]: ExplainResult;
    // not actually supported anymore, but needed for completeness
    [targets.vb]: CodeResult;
};

export type GistGetResult = {
    id: string,
    name: string,
    url: string,
    code: string,
    options: {
        language: LanguageName,
        target: string|undefined,
        release: boolean,
        branchId: string|undefined
    }
};

// Zero-width space (\u200B) is invisible, but ensures that these will be sorted after other files
const sortWeight = '\u200B';
const optionsFileName = sortWeight + sortWeight + '.sharplab.json';

const extensionMap = {
    [languages.csharp]: '.cs',
    [languages.vb]:     '.vb',
    [languages.fsharp]: '.fs'
} as const;

async function validateResponseAndParseJsonAsync(response: Response) {
    if (!response.ok) {
        const text = await response.text();
        const error = new Error(`${response.status} ${response.statusText}\r\n${text}`);
        (error as { json?: unknown }).json = JSON.parse(text);
        throw error;
    }

    return response.json() as Promise<{
        readonly id: string;
        readonly html_url: string;
        readonly files: {
            readonly [key: string]: {
                readonly content: string;
            }|undefined;
        };
    }>;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const readableJson = (o: any) => JSON.stringify(o, null, 4);

function prepareResultForGist<TTarget extends keyof TargetResultMap>(target: TTarget, result: TargetResultMap[TTarget]|ErrorResult): {
    readonly suffix: string;
    readonly content: string;
};
function prepareResultForGist(target: string, result: Result): {
    readonly suffix: string;
    readonly content: string;
};
function prepareResultForGist(target: string, result: Result) {
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
            return { suffix: '.output.txt', content: renderOutputToText((result as unknown as RunResult).value) };
        case targets.verify:
            return { suffix: '.verify.txt', content: result.value };
        case targets.explain:
            return { suffix: '.explained.json', content: readableJson(result.value) };
        default:
            return { suffix: '.processed.json', content: readableJson(result.value) };
    }
}

export async function getGistAsync(id: string): Promise<GistGetResult> {
    const gist = await validateResponseAndParseJsonAsync(await fetch(`https://api.github.com/gists/${id}`));

    const [codeFileName, codeFile] = Object.entries(gist.files)[0] as [string, { content: string }];
    const name = codeFileName.replace(/\.[^.]+$/, '');
    const language = (
        Object.entries(extensionMap).find(
            ([, value]) => codeFileName.endsWith(value)
        ) ?? [languages.csharp] as const
    )[0] as LanguageName;

    const optionsFile = gist.files[optionsFileName];
    const gistOptions = optionsFile ? JSON.parse(optionsFile.content) as {
        target?: string;
        mode?: string;
        branch?: string;
    } : {};

    const options = {
        language,
        target: gistOptions.target,
        release: gistOptions.mode === 'Release',
        branchId: gistOptions.branch
    };

    return {
        id,
        name,
        url: gist.html_url,
        code: codeFile.content,
        options
    };
}

export async function createGistAsync(
    { name, code, options, result }: Pick<Gist, 'name'|'code'|'options'> & { result: Result }
) {
    if (!token)
        throw new Error("Can't save Gists without GitHub auth.");

    const extension = asLookup(extensionMap)[options.language];
    if (name.endsWith(extension as string))
        name = name.substr(0, name.length - (extension as string).length);

    const codeFileName = name + extension;

    const gistOptions = extendType({
        version: 1,
        target: options.target,
        mode: options.release ? 'Release' : 'Debug'
    } as const)<{ branch?: string }>();
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
    } as Gist;
}