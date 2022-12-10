import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../../shared/helpers/testing/recoilTestState';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { GroupOutput } from './GroupOutput';

export default {
    component: GroupOutput,
    decorators: [
        (Story: () => JSX.Element) => <RecoilRoot><Story /></RecoilRoot>
    ]
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
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;

export const LimitReached = () => <GroupOutput group={{
    type: 'inspection:group',
    title: 'Test Group',
    inspections: [
        { type: 'inspection:simple', title: 'Simple', value: 'Test' }
    ],
    limitReached: true
}} />;
export const LimitReachedDarkMode = () => <DarkModeRoot><LimitReached /></DarkModeRoot>;