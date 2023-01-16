import React from 'react';
import { Loader } from './Loader';
import { darkModeStory } from './testing/darkModeStory';

export default {
    component: Loader
};

export const Loading = () => <Loader loading />;
export const LoadingDarkMode = darkModeStory(Loading);
export const LoadingInline = () => <Loader loading inline />;
export const Inactive = () => <Loader />;