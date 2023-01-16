import React from 'react';
import { TestSetRecoilState } from '../../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../../shared/helpers/testing/TestWaitForRecoilStates';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_IL } from '../../../shared/languages';
import type { SimpleInspection } from '../../../shared/resultTypes';
import { codeState } from '../../../shared/state/codeState';
import { languageOptionState } from '../../../shared/state/languageOptionState';
import { darkModeStory } from '../../../shared/testing/darkModeStory';
import { SimpleOutput } from './SimpleOutput';

export default {
    component: SimpleOutput
};

type TemplateProps = {
    inspection: SimpleInspection;
    language?: LanguageName;
};
const Template: React.FC<TemplateProps> = ({ inspection, language = LANGUAGE_CSHARP }) => <>
    <TestSetRecoilState state={languageOptionState} value={language} />
    <TestSetRecoilState state={codeState} value={''} />
    <TestWaitForRecoilStates states={[languageOptionState, codeState]}>
        <SimpleOutput inspection={inspection} />
    </TestWaitForRecoilStates>
</>;

export const Default = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Simple',
    value: 'Test'
}} />;
export const DefaultDarkMode = darkModeStory(Default);

export const Multiline = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Simple',
    value: 'Line 1\r\nLine 2'
}} />;
export const MultilineDarkMode = darkModeStory(Multiline);

export const TitleOnly = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Test'
}} />;
export const TitleOnlyDarkMode = darkModeStory(TitleOnly);

export const Exception = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: 'Test Exception'
}} />;
export const ExceptionDarkMode = darkModeStory(Exception);

export const MultilineException = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: 'Test Exception\r\n  at test location'
}} />;
export const MultilineExceptionDarkMode = darkModeStory(MultilineException);

export const ExceptionNoticeBadImageException = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Exception',
    value: `System.BadImageFormatException: Bad IL format.
   at Program.<<Main>$>g__Test|0_0[T]()
   at Program.<Main>$(String[] args)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)`
}} />;
export const ExceptionNoticeBadImageExceptionDarkMode = darkModeStory(ExceptionNoticeBadImageException);
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
export const WarningDarkMode = darkModeStory(Warning);

export const MultilineWarning = () => <Template inspection={{
    type: 'inspection:simple',
    title: 'Warning',
    value: 'Test Warning\r\n  at test location'
}} />;
export const MultilineWarningDarkMode = darkModeStory(MultilineWarning);