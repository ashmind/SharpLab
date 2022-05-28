import React, { useEffect } from 'react';
import ReactDOM from 'react-dom';
import { RecoilRoot } from 'recoil';
import { useMockBranches } from '../../.storybook/__mocks__/branchesPromise';
import { UserTheme, userThemeState } from '../features/dark-mode/themeState';
import { EXAMPLE_BRANCH } from '../features/roslyn-branches/BranchDetailsSection.stories';
import { branchOptionState } from '../features/roslyn-branches/branchOptionState';
import type { Branch } from '../features/roslyn-branches/types';
import { MOBILE_VIEWPORT } from '../shared/helpers/testing/mobileViewport';
import { recoilTestState } from '../shared/helpers/testing/recoilTestState';
import { getDefaultCode } from '../shared/defaults';
import { LanguageName, LANGUAGE_CSHARP } from '../shared/languages';
import type { UpdateResult } from '../shared/resultTypes';
import { loadedCodeState } from '../shared/state/loadedCodeState';
import { languageOptionState } from '../shared/state/languageOptionState';
import { onlineState } from '../shared/state/onlineState';
import type { ResultUpdateAction } from '../shared/state/results/ResultUpdateAction';
import { targetOptionState } from '../shared/state/targetOptionState';
import { TargetName, TARGET_CSHARP } from '../shared/targets';
import { ResultRoot } from '../shared/testing/ResultRoot';
import { Body } from './Body';
import { EXAMPLE_ERRORS } from './ErrorsSection.stories';
import { EXAMPLE_CSHARP_CODE } from './results/CodeView.stories';
import { EXAMPLE_WARNINGS } from './WarningsSection.stories';

export default {
    component: Body
};

// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
const main = document.querySelector('main')!;
// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
const footer = document.querySelector('footer')!;

type TemplateProps = {
    branch?: Branch;
    result?: Partial<UpdateResult>;
    offline?: boolean;
    dark?: boolean;
};

const Template: React.FC<TemplateProps> = ({ branch, result, offline, dark }) => {
    useMockBranches(branch ? [branch] : []);
    useEffect(() => {
        main.removeAttribute('hidden');
        footer.removeAttribute('hidden');
        return () => {
            main.setAttribute('hidden', 'hidden');
            footer.setAttribute('hidden', 'hidden');
        };
    }, []);

    const resultAction: ResultUpdateAction = {
        type: 'cachedResult',
        target: TARGET_CSHARP,
        updateResult: {
            diagnostics: [],
            cached: { date: new Date('2000-01-01T00:00:00.000Z') },
            x: EXAMPLE_CSHARP_CODE,
            ...result
        }
    };

    const body = ReactDOM.createPortal(
        <RecoilRoot initializeState={recoilTestState(
            [languageOptionState, LANGUAGE_CSHARP as LanguageName],
            [targetOptionState, TARGET_CSHARP as TargetName],
            [loadedCodeState, getDefaultCode(LANGUAGE_CSHARP, TARGET_CSHARP)],
            [userThemeState, (dark ? 'dark' : 'light') as UserTheme],
            [branchOptionState, branch ?? null],
            [onlineState, !offline]
        )}>
            <ResultRoot action={resultAction}>
                <Body />
            </ResultRoot>
        </RecoilRoot>,
        main
    );

    return <>
        <style>{`
            body {
                display: flex !important;
            }
        `}</style>
        {body}
    </>;
};

const mobile = (story: () => JSX.Element) => {
    const result = () => story();
    result.parameters = {
        viewport: MOBILE_VIEWPORT,
        layout: 'fullscreen'
    };
    return result;
};

export const Default = () => <Template />;
export const DefaultDarkMode = () => <Template dark />;

export const Errors = () => <Template result={{ diagnostics: EXAMPLE_ERRORS }} />;
export const ErrorsDarkMode = () => <Template result={{ diagnostics: EXAMPLE_ERRORS }} dark />;

export const Warnings = () => <Template result={{ diagnostics: EXAMPLE_WARNINGS }} />;
export const WarningsDarkMode = () => <Template result={{ diagnostics: EXAMPLE_WARNINGS }} dark />;

export const Offline = () => <Template offline />;
export const OfflineDarkMode = () => <Template offline dark />;

export const WithBranch = () => <Template branch={EXAMPLE_BRANCH} />;
export const WithBranchDarkMode = () => <Template branch={EXAMPLE_BRANCH} dark />;

export const Mobile = mobile(Default);
export const MobileDarkMode = mobile(DefaultDarkMode);
export const MobileErrors = mobile(Errors);
export const MobileErrorsDarkMode = mobile(ErrorsDarkMode);
export const MobileWarnings = mobile(Warnings);
export const MobileWarningsDarkMode = mobile(WarningsDarkMode);
export const MobileOffline = mobile(Offline);
export const MobileOfflineDarkMode = mobile(OfflineDarkMode);