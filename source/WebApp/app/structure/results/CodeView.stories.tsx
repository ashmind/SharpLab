import React from 'react';
import { TargetLanguageName, TARGET_ASM, TARGET_CSHARP, TARGET_IL } from '../../shared/targets';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import { CodeView, LinkedCodeRange } from './CodeView';

export default {
    component: CodeView,
    excludeStories: /^EXAMPLE_/
};

type TemplateProps = {
    code: string;
    language: TargetLanguageName;
    ranges?: ReadonlyArray<LinkedCodeRange>;
};

const Template: React.FC<TemplateProps> = ({ code, language, ranges }) =>
    <CodeView code={code} language={language} ranges={ranges} />;

const EXAMPLE_CSHARP_CODE = `
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue | DebuggableAttribute.DebuggingModes.DisableOptimizations)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
public class C
{
    public void M()
    {
    }
}
`.trim();

// Workaround for .trim() issue https://github.com/storybookjs/storybook/issues/18350
export { EXAMPLE_CSHARP_CODE as EXAMPLE_CSHARP_CODE };

export const CSharp = () => <Template language={TARGET_CSHARP} code={EXAMPLE_CSHARP_CODE} />;
CSharp.storyName = 'C#';
export const CSharpDarkMode = darkModeStory(CSharp);

export const IL = () => <Template language={TARGET_IL} code={`
.class public auto ansi beforefieldinit C
    extends [System.Runtime]System.Object
{
    // Methods
    .method public hidebysig
        instance void M () cil managed
    {
        // Method begins at RVA 0x2050
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: nop
        IL_0001: ret
    } // end of method C::M

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Method begins at RVA 0x2053
        // Code size 8 (0x8)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    } // end of method C::.ctor

} // end of class C
`.trim()} />;
export const ILDarkMode = darkModeStory(IL);

export const JitAsm = () => <Template language={TARGET_ASM} code={`
; Core CLR 6.0.322.12309 on amd64

C..ctor()
    L0000: push rbp
    L0001: sub rsp, 0x20
    L0005: lea rbp, [rsp+0x20]
    L000a: mov [rbp+0x10], rcx
    L000e: cmp dword ptr [0x7ff7c494c2f0], 0
    L0015: je short L001c
    L0017: call 0x00007ff81a26c9f0
    L001c: mov rcx, [rbp+0x10]
    L0020: call 0x00007ff7ba590028
    L0025: nop
    L0026: nop
    L0027: add rsp, 0x20
    L002b: pop rbp
    L002c: ret

C.M()
    L0000: push rbp
    L0001: sub rsp, 0x20
    L0005: lea rbp, [rsp+0x20]
    L000a: mov [rbp+0x10], rcx
    L000e: cmp dword ptr [0x7ff7c494c2f0], 0
    L0015: je short L001c
    L0017: call 0x00007ff81a26c9f0
    L001c: nop
    L001d: nop
    L001e: add rsp, 0x20
    L0022: pop rbp
    L0023: ret
`.trim()} />;
JitAsm.storyName = 'JIT ASM';
export const JitAsmDarkMode = darkModeStory(JitAsm);