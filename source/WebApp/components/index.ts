import './app-code-edit';
import './app-gist-manager';
import './app-section-branch-details';
import './app-select-branch';
import './app-select-language';
import './app-results-section';
import { WarningsSection } from 'app/results/diagnostics/WarningsSection';
import { Diagnostic } from 'app/results/diagnostics/Diagnostic';
import { Footer } from 'app/Footer';
import { MobileSettings } from 'app/mobile/MobileSettings';

/* eslint-disable @typescript-eslint/no-explicit-any */
export const reactComponents = {
    'react-mobile-settings': MobileSettings as any,
    'react-warnings-section': WarningsSection as any,
    'react-diagnostic': Diagnostic as any,
    'react-footer': Footer as any
} as const;
/* eslint-restore @typescript-eslint/no-explicit-any */