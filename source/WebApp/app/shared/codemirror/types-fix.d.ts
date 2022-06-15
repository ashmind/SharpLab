declare namespace CodeMirror {
    function runMode(
        text: string,
        modespec: string | object,
        callback: (HTMLElement | ((text: string, style: string | null) => void)),
        options? : {
            tabSize?: number;
            state?: unknown;
        }
    ): void;

    interface Editor extends Doc {
        toTextArea(): void;
    }
}