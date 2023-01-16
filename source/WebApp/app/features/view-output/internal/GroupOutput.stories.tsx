import React from 'react';
import { darkModeStory } from '../../../shared/testing/darkModeStory';
import { GroupOutput } from './GroupOutput';

export default {
    component: GroupOutput
};

export const Full = () => <GroupOutput group={{
    type: 'inspection:group',
    title: 'Test Group',
    inspections: [
        { type: 'inspection:simple', title: 'Simple', value: 'Test 1' },
        {
            type: 'inspection:group', title: 'Nested Group', inspections: [
                { type: 'inspection:simple', title: 'Simple', value: 'Test 2' }
            ]
        },
        { type: 'inspection:simple', title: 'Exception', value: 'Exception\r\n  at test location' },
        { type: 'inspection:simple', title: 'Warning', value: 'Warning\r\n  at test location' }
    ]
}} />;
export const FullDarkMode = darkModeStory(Full);

export const LimitReached = () => <GroupOutput group={{
    type: 'inspection:group',
    title: 'Test Group',
    inspections: [
        { type: 'inspection:simple', title: 'Simple', value: 'Test' }
    ],
    limitReached: true
}} />;
export const LimitReachedDarkMode = darkModeStory(LimitReached);