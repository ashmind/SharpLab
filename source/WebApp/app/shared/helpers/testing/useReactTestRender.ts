import React, { useEffect, useState } from 'react';
import TestRenderer from 'react-test-renderer';

export const useReactTestRender = <TResult>(
    createElements: () => JSX.Element,
    extractResult: (renderer: TestRenderer.ReactTestRenderer) => TResult,
    deps: React.DependencyList
) => {
    const [result, setResult] = useState<TResult>();
    useEffect(() => {
        let renderer: TestRenderer.ReactTestRenderer|undefined;
        void(TestRenderer.act(() => {
            renderer = TestRenderer.create(createElements());
        }));
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        setResult(extractResult(renderer!));
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, deps);
    return result;
};