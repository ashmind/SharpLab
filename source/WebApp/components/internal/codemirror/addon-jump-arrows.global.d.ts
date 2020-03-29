interface JumpData {
    fromLine: number;
    toLine: number;
    options?: JumpOptions;
}

interface JumpOptions {
    readonly throw?: boolean;
}

declare namespace CodeMirror {
    interface Editor {
        addJumpArrow(fromLine: number, toLine: number, options?: JumpOptions): void;
        setJumpArrows(arrows: JumpData[]): void;
        clearJumpArrows(arrows: JumpData[]): void;
    }
}