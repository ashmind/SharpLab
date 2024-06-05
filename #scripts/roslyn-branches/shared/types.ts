type BaseBranch = {
    readonly id: string;
    readonly name: string;
    readonly group: string;
    readonly url: string;
    readonly sharplab?: {
        readonly supportsUnknownOptions: boolean;
    };
};

type PlatformBranch = BaseBranch & {
    readonly kind: 'platform';
};

type BaseRoslynBranch = BaseBranch & {
    readonly kind: 'roslyn';
    readonly feature?: {
        readonly language: string;
        readonly name: string;
        readonly url: string;
    };
    readonly commits: ReadonlyArray<Commit>;
};

export type ActiveRoslynBranch = BaseRoslynBranch & {
    readonly merged?: undefined;
};

export type MergedRoslynBranch = BaseRoslynBranch & {
    readonly merged: true;
    readonly mergeDetected: string;
    readonly sharplab?: {
        readonly stopped?: undefined;
    } | {
        readonly stopped: string;
        readonly deleted?: string;
    }
};

export type RoslynBranch = ActiveRoslynBranch | MergedRoslynBranch;
export type Branch = PlatformBranch | RoslynBranch;

export type Commit = {
    readonly date: string;
    readonly message: string;
    readonly author: string;
    readonly hash: string;
};

export type CleanupAction =
    | 'fail-not-merged'
    | 'mark-as-merged'
    | 'wait'
    | 'stop'
    | 'delete'
    | 'done';