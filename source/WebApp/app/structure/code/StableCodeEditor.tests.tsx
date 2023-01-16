import React from 'react';
import TestRenderer, { TestRendererOptions } from 'react-test-renderer';
import mirrorsharp from 'mirrorsharp';
import { RecoilRoot } from 'recoil';
import { branchOptionState } from '../../features/roslyn-branches/branchOptionState';
import type { Branch } from '../../features/roslyn-branches/types';
import { DeepPartial, fromPartial } from '../../shared/helpers/testing/fromPartial';
import { LanguageName, LANGUAGE_CSHARP } from '../../shared/languages';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { StableCodeEditor } from './StableCodeEditor';

const RENDERER_OPTIONS: TestRendererOptions = {
    createNodeMock: () => ({})
};

jest.mock('mirrorsharp');
const mirrorsharpMocked = mirrorsharp as jest.MockedFunction<typeof mirrorsharp>;

// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};

const render = ({
    branch,
    loadedCode = '_'
}: {
    branch?: DeepPartial<Branch> | null;
    loadedCode?: string;
} = {}) => {
    const branchValue = branch ? fromPartial<Branch>(branch) : null;
    return <RecoilRoot>
        <TestSetRecoilState state={languageOptionState} value={LANGUAGE_CSHARP as LanguageName} />
        <TestSetRecoilState state={branchOptionState} value={branchValue} />
        <TestSetRecoilState state={loadedCodeState} value={loadedCode} />
        <TestWaitForRecoilStates states={[languageOptionState, branchOptionState, loadedCodeState]}>
            <StableCodeEditor
                initialCached={true}
                executionFlow={null}
                onCodeChange={doNothing}
                onConnectionChange={doNothing}
                onServerError={doNothing}
                onSlowUpdateResult={doNothing}
                onSlowUpdateWait={doNothing} />
        </TestWaitForRecoilStates>
    </RecoilRoot>;
};

const mockMirrorSharpInstance = () => {
    const instance = ({
        setText: jest.fn(),
        setServerOptions: jest.fn(),
        connect: jest.fn(),
        destroy: jest.fn(),
        getCodeMirror: () => ({
            getValue: () => null,
            on: doNothing,
            off: doNothing,
            setJumpArrows: doNothing,
            getWrapperElement: () => ({
                querySelector: () => null
            })
        })
    });
    mirrorsharpMocked.mockReturnValue(fromPartial(instance));
    return instance;
};

it('preserves user code when changing service url', async () => {
    const instanceMock = mockMirrorSharpInstance();
    const component = TestRenderer.create(render(), RENDERER_OPTIONS);
    instanceMock.setText.mockClear();

    await TestRenderer.act(() => {
        component.update(render({ branch: { url: 'new-branch' } }));
    });

    expect(instanceMock.setText).not.toBeCalled();
});

it('updates code if loadedCode changed after initial load', async () => {
    const instanceMock = mockMirrorSharpInstance();
    const component = TestRenderer.create(render(), RENDERER_OPTIONS);
    instanceMock.setText.mockClear();

    await TestRenderer.act(() => {
        component.update(render({ loadedCode: 'new-code' }));
    });

    expect(instanceMock.setText).toBeCalledWith('new-code');
    expect(instanceMock.setText).toBeCalledTimes(1);
});