// "2 +" is for the ts-node-script
export const safeGetArgument = (index: number, name: string) => process.argv[2 + index]
    ?? (() => { throw new Error(`${name} was not provided`); })();