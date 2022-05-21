import { asLookup } from '../../../../helpers/asLookup';
import { assertType } from '../../../../helpers/assertType';
import { LANGUAGE_CSHARP, LANGUAGE_VB, LANGUAGE_FSHARP, LANGUAGE_IL, type LanguageName } from '../../../../shared/languages';
import { TARGET_CSHARP, TARGET_VB, TARGET_IL, TARGET_ASM, TARGET_AST, TARGET_RUN, TARGET_VERIFY, TARGET_EXPLAIN, type TargetName } from '../../../../shared/targets';

const mapFromObject = <TObject, TNewKey extends string, TNewValue>(
    object: TObject,
    mapEntry: (key: keyof TObject, value: TObject[keyof TObject]) => readonly [TNewKey, TNewValue]
) => {
    const result = {} as { [key in TNewKey]: TNewValue };
    for (const key in object) {
        const [newKey, newValue] = mapEntry(key, object[key]);
        result[newKey] = newValue;
    }
    return result;
};

function reverseMap<TMap extends { [key: string]: string }>(map: TMap) {
    type KeyFromValue<T, V> = { [K in keyof T]: V extends T[K] ? K : never }[keyof T];
    return mapFromObject(map, (key, value) => [value, key]) as {
        [TValue in TMap[keyof TMap]]: KeyFromValue<TMap, TValue>
    };
}

const languageMap = {
    [LANGUAGE_CSHARP]: 'cs',
    [LANGUAGE_VB]:     'vb',
    [LANGUAGE_FSHARP]: 'fs',
    [LANGUAGE_IL]:     'il'
} as const;
assertType<{ [K in LanguageName]: string }>(languageMap);

const languageMapReverse = reverseMap(languageMap);

const languageMapAsLookup = asLookup(languageMap);
const languageMapReverseAsLookup = asLookup(languageMapReverse);
export {
    languageMapAsLookup as languageMap,
    languageMapReverseAsLookup as languageMapReverse
};

const targetMap = {
    [TARGET_CSHARP]:  languageMap[LANGUAGE_CSHARP],
    [TARGET_VB]:      languageMap[LANGUAGE_VB],
    [TARGET_IL]:      'il',
    [TARGET_ASM]:     'asm',
    [TARGET_AST]:     'ast',
    [TARGET_RUN]:     'run',
    [TARGET_VERIFY]:  'verify',
    [TARGET_EXPLAIN]: 'explain'
} as const;
assertType<{ [K in TargetName]: string }>(targetMap);

const targetMapReverse = reverseMap(targetMap);
const targetMapAsLookup = asLookup(targetMap);
const targetMapReverseAsLookup = asLookup(targetMapReverse);
export {
    targetMapAsLookup as targetMap,
    targetMapReverseAsLookup as targetMapReverse
};

export const targetMapReverseV1 = mapFromObject(targetMapReverseAsLookup, (key, value) => ['>' + key, value] as const); // eslint-disable-line prefer-template