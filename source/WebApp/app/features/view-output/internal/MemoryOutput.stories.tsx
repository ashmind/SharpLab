import React from 'react';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { MemoryOutput } from './MemoryOutput';

export default {
    component: MemoryOutput
};

const BASIC_INSPECTION = {
    type: 'inspection:memory',
    title: 'System.Int64',
    data: [21, 205, 91, 7, 0, 0, 0, 0],
    labels: []
} as const;

export const Basic = () => <MemoryOutput inspection={BASIC_INSPECTION} />;
export const BasicDarkMode = () => <DarkModeRoot><Basic /></DarkModeRoot>;
export const BasicHex = () => <MemoryOutput inspection={BASIC_INSPECTION} initialState={{ mode: 'hex' }} />;
export const BasicChar = () => <MemoryOutput inspection={BASIC_INSPECTION} initialState={{ mode: 'char' }} />;
export const String = () => <MemoryOutput inspection={{
    type: 'inspection:memory',
    title: 'System.String at 0x227349F0',
    labels: [
        { name: 'header', offset: 0, length: 4 },
        { name: 'type handle', offset: 4, length: 4 },
        { name: 'm_stringLength', offset: 8, length: 4 },
        { name: 'm_firstChar', offset: 12, length: 2 }
    ],
    data: [
        0, 0, 0, 128,
        228, 36, 83, 109,
        4, 0, 0, 0,
        97, 0,
        98, 0,
        99, 0,
        100, 0,
        0, 0
    ]
}} initialState={{ mode: 'char' }} />;
export const StructWithNested = () => <MemoryOutput inspection={{
    type: 'inspection:memory',
    title: 'Struct',
    labels: [
        { name: 'a', offset: 0, length: 4 },
        {
            name: 'nested',
            offset: 4,
            length: 8,
            nested: [
                { name: 'b', offset: 4, length: 4 },
                { name: 'c', offset: 8, length: 4 }
            ]
        },
        { name: 'd', offset: 12, length: 4 }
    ],
    data: [
        1, 0, 0, 0,
        2, 0, 0, 0,
        3, 0, 0, 0,
        4, 0, 0, 0
    ]
}} />;
export const StructWithNestedDarkMode = () => <DarkModeRoot><StructWithNested /></DarkModeRoot>;