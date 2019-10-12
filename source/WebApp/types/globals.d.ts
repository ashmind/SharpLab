declare const define: {
    (definition: () => any): void;
    (dependencies: string[], definition: () => any): void;
    amd: true
} | undefined;