import React from 'react';
import { RecoilRoot } from 'recoil';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { useReactTestRender } from '../../shared/helpers/testing/useReactTestRender';
import { onlineState } from '../../shared/state/onlineState';
import { minimalResultAction } from '../../shared/testing/minimalResultAction';
import { ResultRoot } from '../../shared/testing/ResultRoot';
import { UserTheme, userThemeState } from '../dark-mode/themeState';
import { Favicons } from './Favicons';

export default {
    component: Favicons
};

// eslint-disable-next-line @typescript-eslint/ban-types
type TemplateProps = {
    offline?: boolean;
    error?: boolean;
    dark?: boolean;
};

const renderFavicons = ({ offline, error, dark }: TemplateProps) => {
    return <>
        <TestSetRecoilState state={onlineState} value={!offline} />
        <TestSetRecoilState state={userThemeState} value={(dark ? 'dark' : 'light') as UserTheme} />
        <TestWaitForRecoilStates states={[onlineState, userThemeState]}>
            <ResultRoot action={minimalResultAction({ error })}>
                <Favicons />
            </ResultRoot>
        </TestWaitForRecoilStates>
    </>;
};

type FaviconLink = {
    type: 'link';
    props: {
        type: string;
        href: string;
        sizes?: string;
    };
};

const Template: React.FC<TemplateProps> = ({ offline, error, dark }) => {
    const faviconLinks = useReactTestRender(
        () => <RecoilRoot>{renderFavicons({ offline, error, dark })}</RecoilRoot>,
        ({ root }) => root.findAllByType('link'),
        [offline, error, dark]
    ) as ReadonlyArray<FaviconLink> | undefined;

    if (!faviconLinks)
        return null;

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const svg = faviconLinks.find(l => l.props.type === 'image/svg+xml')!;
    const firstColumnStyle = { minWidth: '5em' } as const;
    const rows: Array<React.ReactNode> = [<tr key='svg'>
        <td style={firstColumnStyle}>SVG</td>
        <td><img src={svg.props.href} width="64" height="64" /></td>
    </tr>];

    const nonSvg = faviconLinks.filter(l => l !== svg);
    if (!nonSvg.every(l => l.props.href.startsWith('data:'))) {
        return <>
            <table>
                <tbody>{rows}</tbody>
            </table>
            Default images other than SVG are not available in Storybook at the moment.
        </>;
    }

    rows.push(nonSvg.map((link, index) => <tr key={index}>
        <td style={firstColumnStyle}>{link.props.sizes}</td>
        <td><img src={link.props.href} /></td>
    </tr>));
    return <table>
        <tbody>{rows}</tbody>
    </table>;
};

export const Default = () => <Template />;
export const Error = () => <Template error />;
export const Offline = () => <Template offline />;
export const DarkMode = () => <Template dark />;