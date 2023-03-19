import { ChangeSet, MapMode, StateEffect } from '@codemirror/state';
import { ViewPlugin } from '@codemirror/view';

interface JumpData {
    fromLine: number;
    toLine: number;
    options?: JumpOptions;
}

interface JumpOptions {
    readonly throw?: boolean;
}

interface JumpSvg {
    readonly g: SVGGElement;
    readonly path: SVGPathElement;
    readonly start: SVGCircleElement;
    readonly end: SVGPathElement;
}

interface JumpArrow {
    readonly key: string;
    readonly from: number;
    readonly to: number;
    readonly options: JumpOptions;
    readonly svg: JumpSvg;
}

const createSVGElement = <TTagName extends keyof SVGElementTagNameMap>(tagName: TTagName) => {
    return document.createElementNS('http://www.w3.org/2000/svg', tagName);
};

type BaseAttributes = {
    class?: string;
    width?: string | number;
    height?: string | number;
};
function setAttributes(element: SVGPathElement, attributes: BaseAttributes & { d?: string }): void;
function setAttributes(element: SVGCircleElement, attributes: BaseAttributes & { cx: string | number; cy: string | number; r: string | number }): void;
function setAttributes(element: SVGElement, attributes: BaseAttributes): void;
// eslint-disable-next-line func-style
function setAttributes(element: SVGElement, attributes: BaseAttributes) {
    for (const key in attributes) {
        element.setAttribute(key, attributes[key as keyof typeof attributes] as string);
    }
}

export const setJumpsEffect = StateEffect.define<ReadonlyArray<JumpData>>();
type SetJumpsEffect = ReturnType<(typeof setJumpsEffect)['of']>;

export const jumpArrows = ViewPlugin.define(view => {
    const sizerLeftMargin = 0;
    let svgAdded = false;

    let arrows = {} as Record<string, JumpArrow>;
    const rootSvg = createSVGElement('svg');
    setAttributes(rootSvg, { class: 'CodeMirror-jump-arrow-layer' });
    // const sizer = null as HTMLElement | null;

    const getRepositionArrowActionIfRequired = (arrow: JumpArrow, changes: ReadonlyArray<ChangeSet>) => {
        let [newFrom, newTo]: [number|null, number|null] = [arrow.from, arrow.to];
        for (const change of changes) {
            if (!newFrom || !newTo)
                break;
            newFrom = change.mapPos(newFrom, -1, MapMode.TrackDel);
            newTo = change.mapPos(newTo, -1, MapMode.TrackDel);
        }

        if (newFrom === arrow.from && newTo === arrow.to)
            return;

        if (!newFrom || !newTo) {
            removeArrow(arrow);
            return;
        }

        const newFromLine = view.state.doc.lineAt(newFrom).number;
        const newToLine = view.state.doc.lineAt(newTo).number;

        return () => {
            delete arrows[arrow.key];
            const rendered = renderAndTrackArrow(newFromLine, newToLine, {
                options: arrow.options,
                svgToReuse: arrow.svg
            });
            if (!rendered)
                removeArrowSvg(arrow);
        };
    };

    const renderAndTrackArrow = (
        fromLine: number,
        toLine: number,
        { options, svgToReuse }: {
            options: JumpOptions;
            svgToReuse?: JumpSvg | null;
        }
    ) => {
        const key = fromLine + '->' + toLine;
        if (arrows[key])
            return false;

        const positions = {
            from: getLineStart(fromLine).position,
            to: getLineStart(toLine).position
        };

        const from = getJumpCoordinates(positions.from);
        const to = getJumpCoordinates(positions.to);

        const leftmost = getLeftmostBound(fromLine, toLine);
        let left = leftmost;
        let up = false;
        if (to.y < from.y) {
            // up
            left -= 8;
            up = true;
        }

        if (left < 1)
            left = 1;

        const offsetY = 4;
        const fromY = from.y + offsetY;
        const toY = to.y - offsetY;

        const groupClassName = 'CodeMirror-jump-arrow'
            + ` CodeMirror-jump-arrow-${up ? 'up' : 'down'}`
            + ` CodeMirror-jump-arrow-${options.throw ? 'throw' : 'default'}`;

        const { g, path, start, end } = svgToReuse ?? {
            g: createSVGElement('g'),
            path: createSVGElement('path'),
            start: createSVGElement('circle'),
            end: createSVGElement('path')
        };
        setAttributes(g, { class: groupClassName });
        setAttributes(path, {
            class: 'CodeMirror-jump-arrow-line',
            d: `M ${from.x} ${fromY} H ${left} V ${toY} H ${to.x}`
        });
        setAttributes(start, {
            class: 'CodeMirror-jump-arrow-start',
            cx: from.x, cy: fromY, r: 2
        });
        setAttributes(end, {
            class: 'CodeMirror-jump-arrow-end',
            d: `M ${to.x} ${toY} l -3 -2 v 4 z`
        });
        if (!svgToReuse) {
            g.appendChild(path);
            g.appendChild(start);
            g.appendChild(end);
            rootSvg.appendChild(g);
        }
        arrows[key] = {
            key,
            from: positions.from,
            to: positions.to,
            options,
            svg: { g, path, start, end }
        };
        return true;
    };

    const removeArrow = (arrow: JumpArrow) => {
        removeArrowSvg(arrow);
        delete arrows[arrow.key];
    };

    const removeArrowSvg = (arrow: JumpArrow) => {
        rootSvg.removeChild(arrow.svg.g);
    };

    const getLeftmostBound = (fromLine: number, toLine: number) => {
        const firstLine = Math.min(fromLine, toLine);
        const lastLine = Math.max(fromLine, toLine);

        let leftmost = getLineStart(firstLine);
        for (let line = firstLine + 1; line <= lastLine; line++) {
            const lineStart = getLineStart(line);
            if (lineStart.column < leftmost.column)
                leftmost = lineStart;
        }

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const coords = view.coordsAtPos(leftmost.position)!;
        return (coords.left + sizerLeftMargin) - 15;
    };

    const getJumpCoordinates = (position: number) => {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const coords = view.coordsAtPos(position)!;
        const left = coords.left + sizerLeftMargin;
        return {
            x: Math.round(100 * (left - 5)) / 100,
            y: Math.round(100 * (coords.top + ((coords.bottom - coords.top) / 2))) / 100
        };
    };

    const getLineStart = (lineNumber: number) => {
        const line = view.state.doc.line(lineNumber);
        const column = (/[^\s]/.exec(line.text)?.index ?? 0);
        return { position: line.from + column, column };
    };

    const replaceAllArrows = (jumps: Iterable<JumpData>) => {
        const existing = Object.values(arrows);
        arrows = {};
        let nextIndexToReuse = 0;
        for (const jump of jumps) {
            const svgToReuse = existing[nextIndexToReuse]?.svg;
            const rendered = renderAndTrackArrow(jump.fromLine, jump.toLine, {
                options: jump.options ?? {},
                svgToReuse
            });
            if (rendered)
                nextIndexToReuse += 1;
        }

        for (let i = nextIndexToReuse; i < existing.length; i++) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            removeArrow(existing[i]!);
        }
    };


    return {
        update({ transactions }) {
            if (!svgAdded && view.dom.parentNode) {
                let sizes = { width: 0, height: 0 };
                view.requestMeasure({
                    // eslint-disable-next-line @typescript-eslint/no-empty-function
                    read() {
                        sizes = {
                            width: view.dom.offsetWidth,
                            height: view.dom.offsetHeight
                        };
                    },
                    write() {
                        setAttributes(rootSvg, sizes);
                        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                        view.dom.parentNode!.appendChild(rootSvg);
                        svgAdded = true;
                    }
                });
            }

            const jumpsEffects = transactions
                .map((t, index) => [t.effects.find(e => e.is(setJumpsEffect)) as (SetJumpsEffect | undefined), index] as const)
                .filter(([effect]) => effect);
            // cutoff index = text changes before set data should not be considered
            const [jumpsEffect, changesCutoffIndex] = jumpsEffects[jumpsEffects.length - 1] ?? [null, -1];
            const layoutActions = [] as Array<() => void>;
            if (jumpsEffect) {
                layoutActions.push(() => replaceAllArrows(jumpsEffect.value));
            }

            const changes = transactions.slice(changesCutoffIndex + 1).map(t => t.changes);
            for (const arrow of Object.values(arrows)) {
                const reposition = getRepositionArrowActionIfRequired(arrow, changes);
                if (reposition)
                    layoutActions.push(reposition);
            }

            view.requestMeasure({
                read() {
                    for (const layoutAction of layoutActions) {
                        layoutAction();
                    }
                }
            });
        }
    };
});