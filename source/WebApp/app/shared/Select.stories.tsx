import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../helpers/testing/recoilTestState';
import { Select, SelectOptions } from './Select';
import { targetOptionState } from './state/targetOptionState';
import { type TargetName, TARGET_CSHARP, TARGET_RUN } from './targets';
import { TargetSelect } from './TargetSelect';
import { DarkModeRoot } from './testing/DarkModeRoot';

export default {
    component: Select
};

type TemplateProps = {
    value: string;
    options: SelectOptions<string>;
};

// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};
const Template: React.FC<TemplateProps> = ({ value, options }) => {
    return <Select value={value} options={options} onSelect={doNothing} />;
};

export const Default = () => <Template value={'test'} options={[{ value: 'test', label: 'Test' }]} />;
export const InGroup = () => <Template value={'test'} options={[{
    groupLabel: 'Test Group',
    options: [
        { value: 'test', label: 'Test' }
    ]
}]} />;
export const DarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;