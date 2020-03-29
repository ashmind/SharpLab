export type HighlightedRange = {
    start: CodeMirror.Position;
    end: CodeMirror.Position;
}|{
    start: number;
    end: number;
};