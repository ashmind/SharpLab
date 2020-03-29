export default function throwError(message: string): never {
    throw new Error(message);
}