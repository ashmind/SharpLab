import { createMockProxy } from 'jest-mock-proxy';

// eslint-disable-next-line max-statements-per-line
class MockProxyTypeProvider<T> { infer() { return createMockProxy<T>(); } }
export type MockProxy<T> = ReturnType<MockProxyTypeProvider<T>['infer']>;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MockFunction<T extends (...args: any) => any> = jest.MockInstance<ReturnType<T>, Parameters<T>>;

type DeepPartial<T> = {
    [P in keyof T]?: DeepPartial<T[P]>;
};

export function fromPartial<T, U extends T = T>(partial: DeepPartial<U>): T {
    return partial as T;
}

type Mutable<T> = {
    -readonly[TKey in keyof T]: T[TKey];
};

export function asMutable<T>(value: T) {
    return value as Mutable<T>;
}