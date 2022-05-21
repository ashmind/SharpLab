import type { CodeResult, AstResult, RunResult, VerifyResult, ExplainResult, ErrorResult, Result } from '../../../../ts/types/results';
import { asLookup } from '../../../helpers/asLookup';
import { assertType } from '../../../helpers/assertType';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../../shared/languages';
import { TargetName, TARGET_ASM, TARGET_AST, TARGET_CSHARP, TARGET_EXPLAIN, TARGET_IL, TARGET_RUN, TARGET_VB, TARGET_VERIFY } from '../../../shared/targets';
import { targetMap } from '../../persistent-state/handlers/helpers/language-and-target-maps';
import type { Gist } from '../gist';
import { token } from './githubAuth';
import renderOutputToText from './internal/render-output-to-text';

type TargetResultMap = {
    [TARGET_CSHARP]: CodeResult;
    [TARGET_IL]: CodeResult;
    [TARGET_ASM]: CodeResult;
    [TARGET_AST]: AstResult;
    [TARGET_RUN]: RunResult;
    [TARGET_VERIFY]: VerifyResult;
    [TARGET_EXPLAIN]: ExplainResult;
    // not actually supported anymore, but needed for completeness
    [TARGET_VB]: CodeResult;
};

// Zero-width space (\u200B) is invisible, but ensures that these will be sorted after other files
const sortWeight = '\u200B';
const optionsFileName = sortWeight + sortWeight + '.sharplab.json';

const extensionMap = {
    [LANGUAGE_CSHARP]: '.cs',
    [LANGUAGE_VB]:     '.vb',
    [LANGUAGE_FSHARP]: '.fs',
    [LANGUAGE_IL]:     '.il'
} as const;
assertType<{ [L in LanguageName]: `.${string}` }>(extensionMap);

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
        case TARGET_CSHARP:
            return { suffix: '.decompiled.cs', content: result.value };
        case TARGET_IL:
            return { suffix: '.il', content: result.value };
        case TARGET_ASM:
            return { suffix: '.jit.asm', content: result.value };
        case TARGET_AST:
            return { suffix: '.ast.json', content: readableJson(result.value) };
        case TARGET_RUN:
            return { suffix: '.output.txt', content: renderOutputToText((result as unknown as RunResult).value) };
        case TARGET_VERIFY:
            return { suffix: '.verify.txt', content: result.value };
        case TARGET_EXPLAIN:
            return { suffix: '.explained.json', content: readableJson(result.value) };
        default:
            return { suffix: '.processed.json', content: readableJson(result.value) };
    }
}

export async function getGistAsync(id: string): Promise<Gist> {
    const gist = await validateResponseAndParseJsonAsync(await fetch(`https://api.github.com/gists/${id}`));

    const [codeFileName, codeFile] = Object.entries(gist.files)[0] as [string, { content: string }];
    const name = codeFileName.replace(/\.[^.]+$/, '');
    const language = (
        Object.entries(extensionMap).find(
            ([, value]) => codeFileName.endsWith(value)
        ) ?? [LANGUAGE_CSHARP] as const
    )[0] as LanguageName;

    const optionsFile = gist.files[optionsFileName];
    const gistOptions = optionsFile ? JSON.parse(optionsFile.content) as {
        target?: string;
        mode?: string;
        branch?: string;
    } : {};
    const target = gistOptions.target && targetMap[gistOptions.target]
        ? gistOptions.target as TargetName
        : TARGET_CSHARP;

    return {
        id,
        name,
        url: gist.html_url,
        code: codeFile.content,
        options: {
            language,
            target,
            release: gistOptions.mode === 'Release',
            branchId: gistOptions.branch ?? null
        }
    };
}

type CreateGistRequest = Pick<Gist, 'name' | 'code' | 'options'> & {
    readonly result: Result;
};

export async function createGistAsync({ name, code, options, result }: CreateGistRequest): Promise<Gist> {
    if (!token)
        throw new Error("Can't save Gists without GitHub auth.");

    const extension = asLookup(extensionMap)[options.language];
    if (name.endsWith(extension as string))
        name = name.substr(0, name.length - (extension as string).length);

    const codeFileName = name + extension;

    const gistOptions = {
        version: 1,
        target: options.target,
        mode: options.release ? 'Release' : 'Debug',
        ...(options.branchId ? { branch: options.branchId } : {})
    };

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