import './app-code-edit';
import './app-gist-manager';
import './app-section-branch-details';
import './app-mobile-settings';
import './app-results-section';
import { WarningsSection } from 'app/results/diagnostics/WarningsSection';
import { Diagnostic } from 'app/results/diagnostics/Diagnostic';
import { Footer } from 'app/Footer';

/* eslint-disable @typescript-eslint/no-explicit-any */
export const reactComponents = {
    'react-warnings-section': WarningsSection as any,
    'react-diagnostic': Diagnostic as any,
    'react-footer': Footer as any
} as const;
/* eslint-restore @typescript-eslint/no-explicit-any */