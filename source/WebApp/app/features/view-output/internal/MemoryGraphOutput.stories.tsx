import React from 'react';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { MemoryGraphOutput } from './MemoryGraphOutput';

export default {
    component: MemoryGraphOutput
};

export const String = () => <MemoryGraphOutput inspection={{
    type: 'inspection:memory-graph',
    stack: [{ id: 1, offset: 0, size: 4, value: 'String ref' }],
    heap: [{ id: 2, title: 'String', value: 'Test' }],
    references: [{ from: 1, to: 2 }]
}} />;
export const Full = () => <MemoryGraphOutput inspection={{
    type: 'inspection:memory-graph',
    stack: [
        { id: 1, offset: 16, size: 4, title: 'x',  value: '1' },
        { id: 2, offset: 0,  size: 4, title: 'c2', value: 'TestClass ref' },
        { id: 7, offset: 4,  size: 4, title: 'c1', value: 'TestClass ref' }
    ],
    heap: [
        {
            id: 3,
            title: 'TestClass',
            value: 'TestClass',
            nestedNodes: [
                { id: 4, title: 'i', value: '2' },
                { id: 5, title: 's', value: 'String ref' }
            ]
        },
        { id: 6, title: 'String', value: 'Test String' },
        {
            id: 8,
            title: 'TestClass',
            value: 'TestClass',
            nestedNodes: [
                { id: 9, title: 'i', value: '1' },
                { id: 10, title: 's', value: 'String ref' }
            ]
        }
    ],
    references: [
        { from: 5,  to: 6 },
        { from: 2,  to: 3 },
        { from: 10, to: 6 },
        { from: 7,  to: 8 }
    ]
}} />;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;