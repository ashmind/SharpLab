import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
import { onlineState } from '../../shared/state/onlineState';
import { UserTheme, userThemeState } from '../dark-mode/themeState';
import { useReactTestRender } from '../../helpers/testing/useReactTestRender';
import { ResultRoot } from '../../shared/testing/ResultRoot';
import { minimalResultAction } from '../../shared/testing/minimalResultAction';
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
    return <RecoilRoot initializeState={recoilTestState(
        [onlineState, !offline],
        [userThemeState, (dark ? 'dark' : 'light') as UserTheme]
    )}>
        <ResultRoot action={minimalResultAction({ error })}>
            <ThemeColorMeta />
        </ResultRoot>
    </RecoilRoot>;
};

const Template: React.FC<TemplateProps> = ({ offline, error, dark }) => {
    const metaColor = useReactTestRender(
        () => renderMeta({ offline, error, dark }),
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