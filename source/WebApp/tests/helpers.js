import Vue from 'vue';

let error;
Vue.config.errorHandler = function (err, vm, info) {
    error = (typeof err === 'string') ? new Error(err) : err;
    // @ts-ignore
    error.vm = vm;
    // @ts-ignore
    error.info = info;
};

export async function vueNextTickWithErrorHandling() {
    if (error)
        throw error;
    // @ts-ignore
    await Vue.nextTick();
    if (error)
        throw error;
    error = null;
}