import languages from '../../../helpers/languages.js';
import targets from '../../../helpers/targets.js';
import mapObject from '../../../helpers/map-object.js';

export const languageAndTargetMap = {
    [languages.csharp]: 'cs',
    [languages.vb]:     'vb',
    [languages.fsharp]: 'fs',
    [targets.il]:       'il',
    [targets.asm]:      'asm',
    [targets.ast]:      'ast',
    [targets.run]:      'run',
    [targets.verify]:   'verify',
    [targets.explain]:  'explain'
};

/** @type {} */
export const languageAndTargetMapReverse = mapObject(languageAndTargetMap, (key, value) => [value, key]);
export const targetMapReverseV1 = mapObject(languageAndTargetMapReverse, (key, value) => ['>' + key, value]); // eslint-disable-line prefer-template