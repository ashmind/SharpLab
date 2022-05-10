export function fromPartial<T, U extends T = T>(partial: Partial<U>): T {
    return partial as T;
}