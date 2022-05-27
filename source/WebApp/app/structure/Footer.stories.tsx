import React, { useEffect } from 'react';
import { RecoilRoot } from 'recoil';
import { MOBILE_VIEWPORT } from '../helpers/testing/mobileViewport';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { Footer } from './Footer';

export default {
    component: Footer
};

const Template = () => {
    useEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const footer = document.querySelector('footer')!;
        footer.removeAttribute('hidden');
        return () => footer.setAttribute('hidden', 'hidden');
    }, []);
    return <RecoilRoot><Footer /></RecoilRoot>;
};

export const Default = () => <Template />;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;

export const Mobile = () => <Template />;
Mobile.parameters = { viewport: MOBILE_VIEWPORT };
export const MobileDarkMode = () => <DarkModeRoot><Mobile /></DarkModeRoot>;
MobileDarkMode.parameters = { viewport: MOBILE_VIEWPORT };