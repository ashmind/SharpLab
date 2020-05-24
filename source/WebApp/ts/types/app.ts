import type { VueConstructor } from 'vue';
import type { MirrorSharpSlowUpdateResult, MirrorSharpConnectionState } from 'mirrorsharp';
import type { TargetName } from '../helpers/targets';
import type { LanguageName } from '../helpers/languages';
import type { Branch } from './branch';
import type { ServerOptions } from './server-options';
import type { HighlightedRange } from './highlighted-range';
import type { Result, CodeResult, AstResult, ExplainResult, AstItem } from './results';
import type { AstViewRef } from './component-ref-interfaces/ast-view-ref';
import type { Gist } from './gist';
import type { CodeRange } from './code-range';

export type AppTheme = 'light'|'dark'|'auto';

export interface AppOptions {
    language: LanguageName;
    target: TargetName|string;
    release: boolean;
    branch: Branch|null;
}

export interface AppData {
    branches: ReadonlyArray<Branch>;

    loadingDelay?: ReturnType<typeof setTimeout>|null;
    loading: boolean;
    online: boolean;

    code: string;
    options: AppOptions;

    lastLoadedCode?: string;

    serviceUrl: string;

    result?: Result;
    lastResultOfType: {
        code: CodeResult|null;
        ast: AstResult|null;
        explain?: ExplainResult|null;
    };

    highlightedCodeRange: HighlightedRange|null;

    gist: Gist|null|undefined;
}

export interface AppStatus {
    online: boolean;
    error: boolean;
    color: '#4684ee'|'#dc3912'|'#aaa';
}

interface AppComputed {
    serverOptions(this: App): ServerOptions;
    status(this: App): AppStatus;
}

interface AppMethods {
    applyUpdateWait(this: App): void;
    applyUpdateResult(this: App, updateResult: MirrorSharpSlowUpdateResult<Result['value']>): void;
    applyServerError(this: App, message: string): void;
    applyConnectionChange(this: App, connectionState: MirrorSharpConnectionState): void;
    applyCodeViewRange(this: App, range: { source: CodeRange }): void;
    applyAstSelect(this: App, item: AstItem): void;
    applyCursorMove(this: App, getCursorOffset: () => number): void;
    applyGistSave(this: App, gist: Gist): void;
}

export interface AppDefinition {
    readonly data: AppData;
    readonly computed: AppComputed;
    readonly methods: AppMethods;
}

interface AppRefs {
    readonly astView: AstViewRef;
}

export type App = AppData
    & { readonly [TKey in keyof AppComputed]: ReturnType<AppComputed[TKey]> }
    & AppMethods
    & { readonly $refs: AppRefs };
export type AppVue = Omit<App, '$refs'> & InstanceType<VueConstructor>;