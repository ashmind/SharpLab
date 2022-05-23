import type { FC, ReactNode } from 'react';
import ReactDOM from 'react-dom';

const { head } = document;
export const elementsToReplace = [...head.querySelectorAll('[data-react-replace]')];
for (const element of elementsToReplace) {
    element.remove();
}
export { elementsToReplace as replacedHeadElements };

type Props = {
    children: ReactNode;
};

export const DocumentHead: FC<Props> = ({ children }) => {
    return ReactDOM.createPortal(children, head);
};