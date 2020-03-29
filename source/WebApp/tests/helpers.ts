import Vue from 'vue';
import { createMockProxy } from 'jest-mock-proxy';

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

export function asMutable<T>(value: T): { -readonly[TKey in keyof T]: T[TKey] } {
    return value;
}

let error: (Error & { vm?: Vue; info?: string })|undefined|null;
Vue.config.errorHandler = function (err, vm, info) {
    error = (typeof err === 'string') ? new Error(err) : err;
    error.vm = vm;
    error.info = info;
};

export async function vueNextTickWithErrorHandling() {
    if (error)
        throw error;

    await Vue.nextTick();
    if (error)
        throw error;
    error = null;
}