import type Vue from 'vue';
// eslint-disable-next-line no-duplicate-imports
import type { VueConstructor } from 'vue';

export default <Refs>(vue: typeof Vue) => vue as VueConstructor<Vue & { $refs: Refs }>;