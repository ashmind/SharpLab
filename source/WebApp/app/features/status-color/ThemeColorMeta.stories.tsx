import React from 'react';
import { RecoilRoot } from 'recoil';
import { onlineState } from '../../shared/state/onlineState';
import { UserTheme, userThemeState } from '../dark-mode/themeState';
import { useReactTestRender } from '../../shared/helpers/testing/useReactTestRender';
import { ResultRoot } from '../../shared/testing/ResultRoot';
import { minimalResultAction } from '../../shared/testing/minimalResultAction';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { ThemeColorMeta } from './ThemeColorMeta';

export default {
    component: ThemeColorMeta
};

// eslint-disable-next-line @typescript-eslint/ban-types
type TemplateProps = {
    offline?: boolean;
    error?: boolean;
    dark?: boolean;
};

const renderMeta = ({ offline, error, dark }: TemplateProps) => {
    return <>
        <TestSetRecoilState state={onlineState} value={!offline} />
        <TestSetRecoilState state={userThemeState} value={(dark ? 'dark' : 'light') as UserTheme} />
        <TestWaitForRecoilStates states={[onlineState, userThemeState]}>
            <ResultRoot action={minimalResultAction({ error })}>
                <ThemeColorMeta />
            </ResultRoot>
        </TestWaitForRecoilStates>
    </>;
};

const Template: React.FC<TemplateProps> = ({ offline, error, dark }) => {
    const metaColor = useReactTestRender(
        () => <RecoilRoot>{renderMeta({ offline, error, dark })}</RecoilRoot>,
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        ({ root }) => root.find(x => x.type === 'meta' && x.props.name === 'theme-color')!.props.content as string,
        [offline, error, dark]
    );

    return <div style={{ width: '100%', height: '30px', backgroundColor: metaColor }} />;
};

export const Default = () => <Template />;
export const Error = () => <Template error />;
export const Offline = () => <Template offline />;
export const DarkMode = () => <Template dark />;