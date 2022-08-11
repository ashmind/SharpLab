import groupToMap from 'array.prototype.grouptomap';
import { DeepPartial, fromPartial } from '../../../../shared/helpers/testing/fromPartial';
import type { FlowArea } from '../../../../shared/resultTypes';
import type { AreaVisitDetails } from './detailsTypes';
import type { TrackerRoot } from './trackingTypes';

expect.extend({
    htmlMatching(root: Element | undefined | null, html: string) {
        html = html.replace(/>\s+</g, '><').trim();
        if (this.isNot) {
            expect(root?.outerHTML).not.toEqual(html);
        }
        else {
            expect(root?.outerHTML).toEqual(html);
        }

        return { pass: true, message: () => '' };
    }
});

declare global {
    // eslint-disable-next-line @typescript-eslint/no-namespace
    namespace jest {
        interface Expect {
            htmlMatching(html: string): CustomMatcherResult;
        }
    }
}

const mockCodeMirror = () => {
    return {
        addLineWidget: jest.fn<
            ReturnType<CodeMirror.Editor['addLineWidget']>,
            Parameters<CodeMirror.Editor['addLineWidget']>
        >(),
        getLine: () => 'test line'
    };
};

const mockTrackerRoot = (): DeepPartial<TrackerRoot> => {
    return {
        areaMap: new Map()
    };
};

const visitMap = (visits: ReadonlyArray<Omit<DeepPartial<AreaVisitDetails>, 'area'> & { area: DeepPartial<FlowArea> }>) => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    return groupToMap(
        visits,
        v => v.area
    );
};

// eslint-disable-next-line @typescript-eslint/no-empty-function
const noRenderAll = () => {};

let actualRenderAreaWidgets: typeof import('./renderAreaWidgets').renderAreaWidgets;
const renderAreaWidgets = (
    cm: DeepPartial<CodeMirror.Editor>,
    visitMap: ReadonlyMap<DeepPartial<FlowArea>, ReadonlyArray<DeepPartial<AreaVisitDetails>>>,
    root: DeepPartial<TrackerRoot> = mockTrackerRoot(),
    requestRenderAll: () => void = noRenderAll
) => actualRenderAreaWidgets(
    fromPartial(cm),
    fromPartial(visitMap),
    fromPartial(root),
    requestRenderAll
);
beforeEach(async () => {
    jest.resetModules();
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    actualRenderAreaWidgets = (await import('./renderAreaWidgets')).renderAreaWidgets;
});

test('fresh render, one visit', async () => {
    // arrange
    const startLine = 5;
    const cm = mockCodeMirror();
    const map = visitMap([{
        area: { startLine },
        start: { step: {} }
    }]);

    // act
    renderAreaWidgets(cm, map);

    // assert
    expect(cm.addLineWidget).toBeCalledWith(
        startLine - 1,
        expect.htmlMatching(`
            <div class="flow-visit-selector" style="padding-left: 0px;">
                <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">•</label>
            </div>
        `),
        { above: true }
    );
});

test('fresh render, one visit, with notes', async () => {
    // arrange
    const startLine = 5;
    const cm = mockCodeMirror();
    const map = visitMap([{
        area: { startLine },
        start: {
            step: { notes: 'note' }
        }
    }]);

    // act
    renderAreaWidgets(cm, map);

    // assert
    expect(cm.addLineWidget).toBeCalledWith(
        startLine - 1,
        expect.htmlMatching(`
            <div class="flow-visit-selector" style="padding-left: 0px;">
                <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">note</label>
            </div>
        `),
        { above: true }
    );
});

test('fresh render, multiple visits to same area, with notes', async () => {
    // arrange
    const cm = mockCodeMirror();
    const area = { startLine: 5 };
    const map = visitMap([
        { area, start: { step: { notes: 'i: 1' } } },
        { area, start: { step: { notes: 'i: 2' } } },
        { area, start: { step: { notes: 'i: 3' } } }
    ]);

    // act
    renderAreaWidgets(cm, map);

    // assert
    expect(cm.addLineWidget).toBeCalledWith(
        area.startLine - 1,
        expect.htmlMatching(`
            <div class="flow-visit-selector" style="padding-left: 0px;">
                <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 1</label>
                <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 2</label>
                <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 3</label>
            </div>
        `),
        { above: true }
    );
});

test('update, new visit added to area, end order', async () => {
    // arrange
    const cm = mockCodeMirror();
    const area = {};
    const map = visitMap([
        { area, start: { step: { notes: 'i: 1' } }, order: 1 }
    ]);
    const trackerRoot = mockTrackerRoot();
    renderAreaWidgets(cm, map, trackerRoot);
    const element = cm.addLineWidget.mock.calls[0][1];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    map.get(area)!.push({ area, start: { step: { notes: 'i: 2' } }, order: 2 });

    // act
    renderAreaWidgets(cm, map, trackerRoot);

    // assert
    expect(element).toEqual(expect.htmlMatching(`
        <div class="flow-visit-selector" style="padding-left: 0px;">
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 1</label>
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 2</label>
        </div>
    `));
});

test('update, new visit added to area, middle order', async () => {
    // arrange
    const cm = mockCodeMirror();
    const area = {};
    const map = visitMap([
        { area, start: { step: { notes: 'i: 1' } }, order: 1 },
        { area, start: { step: { notes: 'i: 3' } }, order: 3 }
    ]);
    const trackerRoot = mockTrackerRoot();
    renderAreaWidgets(cm, map, trackerRoot);
    const element = cm.addLineWidget.mock.calls[0][1];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    map.get(area)!.push({ area, start: { step: { notes: 'i: 2' } }, order: 2 });

    // act
    renderAreaWidgets(cm, map, trackerRoot);

    // assert
    expect(element).toEqual(expect.htmlMatching(`
        <div class="flow-visit-selector" style="padding-left: 0px;">
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 1</label>
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 2</label>
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 3</label>
        </div>
    `));
});

test('update, new visit added to area, start order', async () => {
    // arrange
    const cm = mockCodeMirror();
    const area = {};
    const map = visitMap([
        { area, start: { step: { notes: 'i: 2' } }, order: 2 }
    ]);
    const trackerRoot = mockTrackerRoot();
    renderAreaWidgets(cm, map, trackerRoot);
    const element = cm.addLineWidget.mock.calls[0][1];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    map.get(area)!.push({ area, start: { step: { notes: 'i: 1' } }, order: 1 });

    // act
    renderAreaWidgets(cm, map, trackerRoot);

    // assert
    expect(element).toEqual(expect.htmlMatching(`
        <div class="flow-visit-selector" style="padding-left: 0px;">
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 1</label>
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 2</label>
        </div>
    `));
});

test('update, visit removed from area', async () => {
    // arrange
    const cm = mockCodeMirror();
    const area = {};
    const map = visitMap([
        { area, start: { step: { notes: 'i: 1' } } },
        { area, start: { step: { notes: 'i: 2' } } }
    ]);
    const trackerRoot = mockTrackerRoot();
    renderAreaWidgets(cm, map, trackerRoot);
    const element = cm.addLineWidget.mock.calls[0][1];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    map.get(area)!.splice(1, 1);

    // act
    renderAreaWidgets(cm, map, trackerRoot);

    // assert
    expect(element).toEqual(expect.htmlMatching(`
        <div class="flow-visit-selector" style="padding-left: 0px;">
            <label class="flow-visit"><input type="radio" name="flow-visit-selector-1">i: 1</label>
        </div>
    `));
});

test('update, area removed', async () => {
    // arrange
    const cm = mockCodeMirror();
    const widget = {
        clear: jest.fn<void, []>()
    };
    cm.addLineWidget.mockReturnValue(fromPartial(widget));
    const area = {};
    const map = visitMap([{ area, start: { step: {} } }]);
    const trackerRoot = mockTrackerRoot();
    renderAreaWidgets(cm, map, trackerRoot);
    map.delete(area);

    // act
    renderAreaWidgets(cm, map, trackerRoot);

    // assert
    expect(widget.clear).toBeCalled();
});