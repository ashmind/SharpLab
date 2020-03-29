import CodeMirror from 'codemirror';

interface JumpData {
    fromLine: number;
    toLine: number;
    options?: JumpOptions;
}

interface JumpOptions {
    readonly throw?: boolean;
}

interface JumpBookmarks {
    readonly from: Readonly<CodeMirror.TextMarker>;
    readonly to: Readonly<CodeMirror.TextMarker>;
}

interface JumpSvg {
    readonly g: SVGGElement;
    readonly path: SVGPathElement;
    readonly start: SVGCircleElement;
    readonly end: SVGPathElement;
}

interface JumpArrow {
    readonly key: string;
    readonly from: Readonly<CodeMirror.Position>;
    readonly to: Readonly<CodeMirror.Position>;
    readonly bookmarks: JumpBookmarks;
    readonly options: JumpOptions;
    readonly svg: JumpSvg;
}

function debounce(func: () => void, interval: number) {
    let timer: ReturnType<typeof setTimeout>|null = null;
    return () => {
        if (timer)
            return;
        timer = setTimeout(() => { func(); timer = null; }, interval);
    };
}

function createSVGElement<TTagName extends keyof SVGElementTagNameMap>(tagName: TTagName) {
    return document.createElementNS('http://www.w3.org/2000/svg', tagName);
}

function setAttributes<TElement extends Element>(
    element: TElement,
    attributes: {
        class?: string;
        width?: string|number;
        height?: string|number;
    } & (
        TElement extends SVGPathElement ? { d?: string } :
        TElement extends SVGCircleElement ? { cx: string|number; cy: string|number; r: string|number } : {}
    )
) {
    for (const key in attributes) {
        element.setAttribute(key, attributes[key as keyof typeof attributes] as string);
    }
}

class ArrowLayer {
    private root: SVGSVGElement;
    private sizer: HTMLElement;
    private cm: CodeMirror.Editor;
    private jumps: {
        [key: string]: JumpArrow|undefined;
    };
    private sizerLeftMargin!: number;

    constructor(cm: CodeMirror.Editor) {
        const wrapper = cm.getWrapperElement();
        const scroll = wrapper.querySelector('.CodeMirror-scroll');
        const sizer = wrapper.querySelector('.CodeMirror-sizer');
        const svg = createSVGElement('svg');
        setAttributes(svg, { class: 'CodeMirror-jump-arrow-layer' });
        this.root = svg;
        this.sizer = sizer as HTMLElement;
        this.resize();
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        scroll!.appendChild(svg);

        this.cm = cm;
        this.jumps = {};
        cm.on('update', debounce(() => this.resize(), 100));
        cm.on('changes', () => this.repositionJumps());
    }

    resize() {
        setAttributes(this.root, {
            width: this.sizer.offsetWidth,
            height: this.sizer.offsetHeight
        });
        this.sizerLeftMargin = parseInt(this.sizer.style.marginLeft);
    }

    repositionJumps() {
        for (const [key, jump] of Object.entries(this.jumps) as ReadonlyArray<[string, JumpArrow]>) {
            const newFrom = jump.bookmarks.from.find()?.from;
            const newTo = jump.bookmarks.to.find()?.from;
            if (!newFrom || !newTo) {
                this.clearJump(jump);
                delete this.jumps[jump.key];
                continue;
            }

            const unchanged = newFrom.line === jump.from.line
                && newFrom.ch === jump.from.ch
                && newTo.ch === jump.to.ch
                && newTo.line === jump.to.line;
            if (unchanged)
                continue;
            delete this.jumps[key];
            const rendered = this.renderJump(newFrom.line, newTo.line, {
                options: jump.options,
                svgToReuse: jump.svg,
                bookmarksToReuse: jump.bookmarks
            });
            if (!rendered)
                this.clearJump(jump);
        }
    }

    renderJump(
        fromLine: number,
        toLine: number,
        { options, svgToReuse, bookmarksToReuse = null }: {
            options: JumpOptions;
            svgToReuse?: JumpSvg|null;
            bookmarksToReuse?: JumpBookmarks|null;
        }
    ) {
        const key = fromLine + '->' + toLine;
        if (this.jumps[key])
            return false;

        const positions = {
            from: { line: fromLine, ch: this.getLineStart(fromLine) },
            to: { line: toLine, ch: this.getLineStart(toLine) }
        };

        const from = this.getJumpCoordinates(positions.from);
        const to = this.getJumpCoordinates(positions.to);

        const leftmost = this.getLeftmostBound(fromLine, toLine);
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
            + (up ? ' CodeMirror-jump-arrow-up' : '')
            + (options.throw ? ' CodeMirror-jump-arrow-throw' : '');

        const { g, path, start, end } = svgToReuse || {
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
            this.root.appendChild(g);
        }
        this.jumps[key] = {
            key,
            from: positions.from,
            to: positions.to,
            bookmarks: {
                from: bookmarksToReuse ? bookmarksToReuse.from : (this.cm as unknown as CodeMirror.Doc).setBookmark(positions.from),
                to: bookmarksToReuse ? bookmarksToReuse.to : (this.cm as unknown as CodeMirror.Doc).setBookmark(positions.to)
            },
            options,
            svg: { g, path, start, end }
        };
        return true;
    }

    clearJump(jump: JumpArrow) {
        this.clearBookmarks(jump);
        this.root.removeChild(jump.svg.g);
    }

    clearBookmarks(jump: JumpArrow) {
        jump.bookmarks.from.clear();
        jump.bookmarks.to.clear();
    }

    getLeftmostBound(fromLine: number, toLine: number) {
        const firstLine = Math.min(fromLine, toLine);
        const lastLine = Math.max(fromLine, toLine);

        let leftmost = { ch: 9999 } as CodeMirror.Position;
        for (let line = firstLine; line <= lastLine; line++) {
            const lineStart = this.getLineStart(line);
            if (lineStart < leftmost.ch)
                leftmost = { line, ch: lineStart };
        }
        const coords = this.cm.cursorCoords(leftmost, 'local');
        return (coords.left + this.sizerLeftMargin) - 15;
    }

    getJumpCoordinates(position: CodeMirror.Position) {
        const coords = this.cm.cursorCoords(position, 'local');
        const left = coords.left + this.sizerLeftMargin;
        return {
            x: Math.round(100 * (left - 5)) / 100,
            y: Math.round(100 * (coords.top + ((coords.bottom - coords.top) / 2))) / 100
        };
    }

    getLineStart(line: number) {
        const match = /[^\s]/.exec((this.cm as unknown as CodeMirror.Doc).getLine(line));
        return match ? match.index : 9999;
    }

    replaceJumps(data: Iterable<JumpData>) {
        const existing = Object.values(this.jumps);
        this.jumps = {};
        let nextIndexToReuse = 0;
        for (const jumpData of data) {
            const jumpToReuse = existing[nextIndexToReuse];
            if (jumpToReuse)
                this.clearBookmarks(jumpToReuse);
            const rendered = this.renderJump(jumpData.fromLine, jumpData.toLine, {
                options: jumpData.options || {},
                svgToReuse: jumpToReuse ? jumpToReuse.svg : null
            });
            if (rendered)
                nextIndexToReuse += 1;
        }

        for (let i = nextIndexToReuse; i < existing.length; i++) {
            this.clearJump(existing[i] as JumpArrow);
        }
    }
}

const STATE_KEY = 'jumpArrowLayer';
CodeMirror.defineExtension('addJumpArrow', function (this: CodeMirror.Editor, fromLine: number, toLine: number, options?: JumpOptions) {
    let layer = this.state[STATE_KEY] as ArrowLayer|undefined;
    if (!layer) {
        layer = new ArrowLayer(this);
        this.state[STATE_KEY] = layer;
    }
    layer.renderJump(fromLine, toLine, { options: options || {} });
});

CodeMirror.defineExtension('setJumpArrows', function(this: CodeMirror.Editor, arrows: JumpData[]) {
    let layer = this.state[STATE_KEY] as ArrowLayer|undefined;
    if (!layer) {
        layer = new ArrowLayer(this);
        this.state[STATE_KEY] = layer;
    }
    layer.replaceJumps(arrows);
});

CodeMirror.defineExtension('clearJumpArrows', function(this: CodeMirror.Editor) {
    const layer = this.state[STATE_KEY] as ArrowLayer|undefined;
    if (!layer)
        return;
    layer.replaceJumps([]);
});