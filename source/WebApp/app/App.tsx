import React from 'react';
import { RecoilRoot } from 'recoil';
import { Favicons } from './features/status-color/Favicons';
import { ThemeColorMeta } from './features/status-color/ThemeColorMeta';
import { DocumentHead } from './helpers/DocumentHead';
import { AppLoader } from './structure/AppLoader';
import { AppStateManager } from './structure/AppStateManager';
import { Body } from './structure/Body';
import { ScrollBarSizeStyleSetup } from './structure/ScrollBarSizeStyleSetup';

export const App: React.FC = () => {
    return <RecoilRoot>
        <DocumentHead>
            <ThemeColorMeta />
            <Favicons />
        </DocumentHead>
        <AppStateManager />
        <ScrollBarSizeStyleSetup />
        <AppLoader>
            <Body />
        </AppLoader>
    </RecoilRoot>;
};