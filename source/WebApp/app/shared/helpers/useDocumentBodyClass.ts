import { useEffect } from 'react';

export const useDocumentBodyClass = (className: string | null | undefined) => useEffect(() => {
    if (!className)
        return;
    document.body.classList.add(className);
    return () => document.body.classList.remove(className);
}, [className]);