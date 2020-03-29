import Vue from 'vue';
import { toMatchImageSnapshot } from 'jest-image-snapshot';

expect.extend({ toMatchImageSnapshot });

jest.setTimeout(3 * 60 * 1000);

Vue.config.warnHandler = (msg, _, trace) => {
    throw new Error(msg + trace);
};