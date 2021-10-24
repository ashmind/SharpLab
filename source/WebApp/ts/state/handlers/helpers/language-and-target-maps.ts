import { languages } from '../../../helpers/languages';
import { targets } from '../../../helpers/targets';
import mapObject from '../../../helpers/map-object';
import asLookup from '../../../helpers/as-lookup';
import { assertType } from '../../../helpers/assert-type';

function reverseMap<TMap extends { [key: string]: string }>(map: TMap) {
    type KeyFromValue<T, V> = { [K in keyof T]: V extends T[K] ? K : never }[keyof T];
    return mapObject(map, (key, value) => [value, key]) as {
        [TValue in TMap[keyof TMap]]: KeyFromValue<TMap, TValue>
    };
}

const languageMap = {
    [languages.csharp]: 'cs',
    [languages.vb]:     'vb',
    [languages.fsharp]: 'fs',
    [languages.il]:     'il'
} as const;
assertType<{ [K in typeof languages[keyof typeof languages]]: string }>(languageMap);

const languageMapReverse = reverseMap(languageMap);

const languageMapAsLookup = asLookup(languageMap);
const languageMapReverseAsLookup = asLookup(languageMapReverse);
export {
    languageMapAsLookup as languageMap,
    languageMapReverseAsLookup as languageMapReverse
};

const targetMap = {
    [targets.csharp]:  languageMap[languages.csharp],
    [targets.vb]:      languageMap[languages.vb],
    [targets.il]:      'il',
    [targets.asm]:     'asm',
    [targets.ast]:     'ast',
    [targets.run]:     'run',
    [targets.verify]:  'verify',
    [targets.explain]: 'explain'
} as const;
assertType<{ [K in typeof targets[keyof typeof targets]]: string }>(targetMap);

const targetMapReverse = reverseMap(targetMap);
const targetMapAsLookup = asLookup(targetMap);
const targetMapReverseAsLookup = asLookup(targetMapReverse);
export {
    targetMapAsLookup as targetMap,
    targetMapReverseAsLookup as targetMapReverse
};

export const targetMapReverseV1 = mapObject(targetMapReverseAsLookup, (key, value) => ['>' + key, value]); // eslint-disable-line prefer-template