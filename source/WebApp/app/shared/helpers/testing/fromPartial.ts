export type DeepPartial<T> = {
    [P in keyof T]?: DeepPartial<T[P]>;
};

export function fromPartial<T, U extends T = T>(partial: DeepPartial<U>): T {
    return partial as T;
}