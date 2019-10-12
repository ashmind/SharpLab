declare namespace CodeMirror {
    interface Editor extends CodeMirror.Doc {
    }
}

interface ParentNode {
    querySelectorAll(selectors: '.CodeMirror'): NodeListOf<Element&{ CodeMirror: CodeMirror.Editor? }>;
}