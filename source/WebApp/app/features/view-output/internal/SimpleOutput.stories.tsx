import React from 'react';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { SimpleOutput } from './SimpleOutput';

export default {
    component: SimpleOutput
};

export const Default = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Simple',
    value: 'Test'
}} />;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;

export const Multiline = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Simple',
    value: 'Line 1\r\nLine 2'
}} />;
export const MultilineDarkMode = () => <DarkModeRoot><Multiline /></DarkModeRoot>;

export const TitleOnly = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Test'
}} />;
export const TitleOnlyDarkMode = () => <DarkModeRoot><TitleOnly /></DarkModeRoot>;

export const Exception = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: 'Test Exception'
}} />;
export const ExceptionDarkMode = () => <DarkModeRoot><Exception /></DarkModeRoot>;

export const MultilineException = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: 'Test Exception\r\n  at test location'
}} />;
export const MultilineExceptionDarkMode = () => <DarkModeRoot><MultilineException /></DarkModeRoot>;

export const Warning = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Warning',
    value: 'Test Warning'
}} />;
export const WarningDarkMode = () => <DarkModeRoot><Warning /></DarkModeRoot>;

export const MultilineWarning = () => <SimpleOutput inspection={{
    type: 'inspection:simple',
    title: 'Warning',
    value: 'Test Warning\r\n  at test location'
}} />;
export const MultilineWarningDarkMode = () => <DarkModeRoot><MultilineWarning /></DarkModeRoot>;