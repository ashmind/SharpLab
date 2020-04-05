export default function asLookup<T>(object: T) {
    return object as string extends keyof T
        ? { readonly [key: string]: T[keyof T]|undefined }
        : { readonly [Key in keyof T]: T[Key]; } & { readonly [key: string]: T[keyof T]|undefined };
}