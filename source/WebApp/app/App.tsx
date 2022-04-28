import React, { FC } from 'react';
import { Footer } from './Footer';
import { Main } from './Main';
import { AppStateProvider } from './main/AppStateProvider';

export const App: FC = () => {
    return <AppStateProvider>
        <Main />
        <Footer />
    </AppStateProvider>;
};