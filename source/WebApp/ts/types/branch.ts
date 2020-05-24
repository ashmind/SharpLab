export interface Branch {
    readonly id: string;
    readonly name: string;
    readonly group: string;
    readonly kind: 'roslyn'|'platform';
    readonly url: string;

    // not received from server, set in code
    readonly displayName?: string;

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
    readonly date: Date;
}