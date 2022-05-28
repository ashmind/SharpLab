import React, { useEffect } from 'react';
import { useRecoilValue } from 'recoil';
import { useDocumentBodyClass } from '../helpers/useDocumentBodyClass';
import { statusSelector } from '../shared/state/statusSelector';
import { Footer } from './Footer';
import { Main } from './Main';

export const Body: React.FC = () => {
    const status = useRecoilValue(statusSelector);
    useDocumentBodyClass(`root-status-${status}`);

    // Note: <Body> is actually under <main>, not <body>,
    // since React does not recommend attaching to <body>.
    // However since both <Main> and <Body> are fragments,
    // and <Footer> is a portal, the final structure still matches
    // <body>
    //    <main>
    //    <footer>
    return <>
        <Main />
        <Footer />
    </>;
};