import Vue from 'vue';
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

export function asMutable<T>(value: T): { -readonly[TKey in keyof T]: T[TKey] } {
    return value;
}

let error: (Error & { vm?: Vue; info?: string })|undefined|null;
// eslint-disable-next-line @typescript-eslint/unbound-method
Vue.config.errorHandler = (err, vm, info) => {
    error = (typeof err === 'string') ? new Error(err) : err;
    error.vm = vm;
    error.info = info;
};

export async function vueNextTickWithErrorHandling() {
    if (error) {
        // eslint-disable-next-line @typescript-eslint/no-throw-literal
        throw error;
    }

    await Vue.nextTick();
    // https://github.com/microsoft/TypeScript/issues/9998
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (error) {
        // eslint-disable-next-line @typescript-eslint/no-throw-literal
        throw error;
    }
    error = null;
}