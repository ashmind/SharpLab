import type { NodeRect } from './types';

export const getOffsetClientRect = (element: HTMLElement, parentRect: NodeRect) => {
    const { top, left, bottom, right, width, height } = element.getBoundingClientRect();
    return {
        top: top - parentRect.top,
        left: left - parentRect.left,
        bottom: bottom - parentRect.top,
        right: right - parentRect.left,
        width, height
    };
};