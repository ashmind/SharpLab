import React from 'react';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { OutputView } from './OutputView';

export default {
    component: OutputView
};

export const Empty = () => <OutputView output={[]} />;
export const EmptyDarkMode = () => <DarkModeRoot><Empty /></DarkModeRoot>;

export const Full = () => <OutputView output={[
    'Console 1\r\n',
    {
        type: 'inspection:simple',
        title: 'Inspect',
        value: 'Inspect'
    },
    'Console 2, \n  Multiline\r\n',
    {
        type: 'inspection:simple',
        title: 'Exception',
        value: 'System.Exception: Exception\r\n   at <Program>$.<Main>$(String[] args)'
    }
]} />;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;