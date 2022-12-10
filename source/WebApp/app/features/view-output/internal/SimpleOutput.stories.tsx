import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../../shared/helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_IL } from '../../../shared/languages';
import type { SimpleInspection } from '../../../shared/resultTypes';
import { codeState } from '../../../shared/state/codeState';
import { languageOptionState } from '../../../shared/state/languageOptionState';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { SimpleOutput } from './SimpleOutput';

export default {
    component: SimpleOutput
};

type TemplateProps = {
    inspection: SimpleInspection;
    language?: LanguageName;
};
const Template: React.FC<TemplateProps> = ({ inspection, language = LANGUAGE_CSHARP }) =>
    <RecoilRoot initializeState={recoilTestState(
        [languageOptionState, language],
        [codeState, '']
    )}>
        <SimpleOutput inspection={inspection} />
    </RecoilRoot>;

export const Default = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Simple',
    value: 'Test'
}} />;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;

export const Multiline = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Simple',
    value: 'Line 1\r\nLine 2'
}} />;
export const MultilineDarkMode = () => <DarkModeRoot><Multiline /></DarkModeRoot>;

export const TitleOnly = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Test'
}} />;
export const TitleOnlyDarkMode = () => <DarkModeRoot><TitleOnly /></DarkModeRoot>;

export const Exception = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: 'Test Exception'
}} />;
export const ExceptionDarkMode = () => <DarkModeRoot><Exception /></DarkModeRoot>;

export const MultilineException = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: 'Test Exception\r\n  at test location'
}} />;
export const MultilineExceptionDarkMode = () => <DarkModeRoot><MultilineException /></DarkModeRoot>;

export const ExceptionNoticeBadImageException = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: `System.BadImageFormatException: Bad IL format.
   at Program.<<Main>$>g__Test|0_0[T]()
   at Program.<Main>$(String[] args)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)`
}} />;
export const ExceptionNoticeBadImageExceptionDarkMode = () => <DarkModeRoot><ExceptionNoticeBadImageException /></DarkModeRoot>;
export const ExceptionNoticeBadImageExceptionAssemblyNotFound = () => <Template language={LANGUAGE_IL} inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: `System.BadImageFormatException: Could not load file or assembly '<Unknown>'. Index not found. (0x80131124)
File name: '<Unknown>'
   at System.Runtime.Loader.AssemblyLoadContext.InternalLoad(ReadOnlySpan\`1 arrAssembly, ReadOnlySpan\`1 arrSymbols)
   at System.Runtime.Loader.AssemblyLoadContext.LoadFromStream(Stream assembly, Stream assemblySymbols)
   at SharpLab.Container.Execution.ExecuteCommandHandler.ExecuteAssembly(Byte[] assemblyBytes) in D:\\a\\SharpLab\\SharpLab\\source\\Container\\Execution\\ExecuteCommandHandler.cs:line 53
   at SharpLab.Container.Execution.ExecuteCommandHandler.Execute(ExecuteCommand command) in D:\\a\\SharpLab\\SharpLab\\source\\Container\\Execution\\ExecuteCommandHandler.cs:line 26`
}} />;

export const Warning = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Warning',
    value: 'Test Warning'
}} />;
export const WarningDarkMode = () => <DarkModeRoot><Warning /></DarkModeRoot>;

export const MultilineWarning = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Warning',
    value: 'Test Warning\r\n  at test location'
}} />;
export const MultilineWarningDarkMode = () => <DarkModeRoot><MultilineWarning /></DarkModeRoot>;