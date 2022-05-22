import React, { FC } from 'react';
import { RecoilRoot } from 'recoil';
import { Favicons } from './features/theme-color/Favicons';
import { ThemeColorMeta } from './features/theme-color/ThemeColorMeta';
import { Footer } from './Footer';
import { Main } from './Main';
import { AppLoader } from './main/AppLoader';
import { AppStateManager } from './main/AppStateManager';
import { ScrollBarSizeStyleSetup } from './main/ScrollBarSizeStyleSetup';
import { DocumentHead } from './shared/DocumentHead';

export const App: FC = () => {
    return <RecoilRoot>
        <DocumentHead>
            <ThemeColorMeta />
            <Favicons />
        </DocumentHead>
        <AppStateManager />
        <ScrollBarSizeStyleSetup />
        <AppLoader>
            <Main />
            <Footer />
        </AppLoader>
    </RecoilRoot>;
};