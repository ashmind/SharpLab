export interface Branch {
    readonly id: string;
    readonly name: string;
    readonly group: string;
    readonly kind: 'roslyn'|'platform';
    readonly url: string;

    // not received from server, set in code
    displayName?: string;

    readonly feature?: {
        readonly language: string;
        readonly name: string;
        readonly url: string;
    };

    readonly commits?: ReadonlyArray<Readonly<BranchCommit>>;
}

export interface BranchCommit {
    readonly message: string;
    readonly author: string;
    readonly hash: string;
    date: Date;
}

export interface BranchGroup {
    readonly name: string;
    readonly kind: Branch['kind'];
    readonly branches: Array<Branch>;
}