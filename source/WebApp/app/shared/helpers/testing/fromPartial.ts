export type DeepPartial<T> = {
    [P in keyof T]?: DeepPartial<T[P]>;
};

export const fromPartial = <T, U extends T = T>(partial: DeepPartial<U>): T => {
    return partial as T;
};