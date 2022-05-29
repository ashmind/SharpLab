import React from 'react';
import { Loader } from './Loader';
import { DarkModeRoot } from './testing/DarkModeRoot';

export default {
    component: Loader
};

export const Loading = () => <Loader loading />;
export const LoadingDarkMode = () => <DarkModeRoot><Loading /></DarkModeRoot>;
export const LoadingInline = () => <Loader loading inline />;
export const Inactive = () => <Loader />;