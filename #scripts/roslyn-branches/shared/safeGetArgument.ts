export const safeGetArgument = <T extends string = string>(index: number, name: string) =>
    // "2 +" is for the ts-node-script
    (process.argv[2 + index] ?? (() => { throw new Error(`${name} was not provided`); })()) as T;