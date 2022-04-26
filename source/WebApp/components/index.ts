import './app-gist-manager';
import './app-section-group-code';
import './app-section-group-results';
import { Footer } from 'app/Footer';
import { MobileSettings } from 'app/mobile/MobileSettings';

/* eslint-disable @typescript-eslint/no-explicit-any */
export const reactComponents = {
    'react-mobile-settings': MobileSettings as any,
    'react-footer': Footer as any
} as const;
/* eslint-restore @typescript-eslint/no-explicit-any */