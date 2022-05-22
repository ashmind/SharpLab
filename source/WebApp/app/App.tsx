import React, { FC } from 'react';
import { RecoilRoot } from 'recoil';
import { Footer } from './Footer';
import { Main } from './Main';
import { AppLoader } from './main/AppLoader';
import { AppStateManager } from './main/AppStateManager';
import { ScrollBarSizeStyleSetup } from './main/ScrollBarSizeStyleSetup';

export const App: FC = () => {
    return <RecoilRoot>
        <AppStateManager />
        <ScrollBarSizeStyleSetup />
        <AppLoader>
            <Main />
            <Footer />
        </AppLoader>
    </RecoilRoot>;
};