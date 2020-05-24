import Vue from 'vue';
import type { AppDefinition, AppData } from '../types/app';
import './mixins/format-date';
import './mixins/markdown';
import { allHooks, Hooks } from './hooks/registry';
import '../../components/index';
import './directives/app-class-toggle';
import './hooks/app-cloak';
import './hooks/app-mobile-codemirror-fullscreen';
import './hooks/app-theme-color-manager';

type WatchParameters = Parameters<Vue['$watch']>;
type WatchMap = AppData & {
    'options.branch': AppData['options']['branch'];
    'options.language': AppData['options']['language'];
    'options.target': AppData['options']['target'];
};
interface UI {
    watch<TKey extends keyof WatchMap>(name: TKey, callback: (newValue: WatchMap[TKey], oldValue: WatchMap[TKey]) => void, options?: WatchParameters[2]): void;
}

const documentReadyPromise = new Promise(resolve => {
    document.addEventListener('DOMContentLoaded', () => resolve());
});

async function createUIAsync(app: AppDefinition) {
    const main = await createTopLevelUIComponentAsync(app, 'main', allHooks.main);
    await createTopLevelUIComponentAsync(app, 'main + footer', allHooks.footer);

    return main;
}

function createTopLevelUIComponentAsync(app: AppDefinition, selector: string, hooks: Partial<Hooks> = {}) {
    return new Promise<UI>((resolve, reject) => {
        try {
            // eslint-disable-next-line no-new
            new Vue({
                el:       selector,
                data:     app.data,
                computed: app.computed,
                methods:  app.methods,
                mounted() {
                    Vue.nextTick(() => {
                        for (const hook of hooks.ready ?? []) {
                            hook(this);
                        }
                        resolve(wrap(this));
                    });
                }
            });
        }
        catch (e) {
            reject(e);
        }
    });
}

function wrap(vue: Vue) {
    return {
        watch: (name, callback, options) => {
            vue.$watch(name, callback, options);
        }
    } as UI;
}

export default async function(app: AppDefinition) {
    await documentReadyPromise;
    return createUIAsync(app);
}