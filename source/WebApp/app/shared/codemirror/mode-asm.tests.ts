import CodeMirror from 'codemirror';
import { fromPartial } from '../../shared/helpers/testing/fromPartial';
import './mode-asm';

test.each([
    [ 'vmovdqa', `L0011: vmovdqa [rbp-0x30], xmm4
        L0016: vmovdqa [rbp-0x20], xmm4
        L001b: vmovdqa [rbp-0x10], xmm4`],
    [ 'je', 'L000e: je short L0015' ],
    [ 'jle', `L0015: jle short L0001` ]
] as const)('mode-asm codemirror highlights assembly instructions correctly', async (expectedToken: string, assemblyCode: string) => {

    Range.prototype.getBoundingClientRect = () => fromPartial({});
    Range.prototype.getClientRects = () => fromPartial({ length: 0 });

    const textarea = document.createElement('textarea');
    document.body.appendChild(textarea);
    const cm = CodeMirror.fromTextArea(textarea, {
        readOnly: true,
        mode: 'text/x-asm'
    });

    const renderingListener = jest.fn();
    cm.on('renderLine', renderingListener);
    cm.setValue(assemblyCode);
    expect(renderingListener).toBeCalled();
    for (const [,, element] of renderingListener.mock.calls) {
        expect((element as HTMLElement).innerHTML).toContain(`<span class="cm-builtin">${expectedToken}</span>`);
    }
});