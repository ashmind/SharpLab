export type Branch = {
    id: string;
    name: string;
    group: string;
    url: string;
    sharplab?: {
        supportsUnknownOptions: boolean;
    };
} & ({
    kind: 'platform';
} | {
    kind: 'roslyn';
    feature?: {
        language: string;
        name: string;
        url: string;
    };
    commits: Array<{
        date: string;
        message: string;
        author: string;
        hash: string;
    }>;
});

export type CleanupBranch = {    
    branch: string;
    commit: string;
    app: string;
};