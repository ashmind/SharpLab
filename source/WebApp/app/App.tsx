import React, { FC } from 'react';
import { Footer } from './Footer';
import { Main } from './Main';
import { AppStateManager } from './main/AppStateManager';

export const App: FC = () => {
    return <AppStateManager>
        <Main />
        <Footer />
    </AppStateManager>;
};