import { LANGUAGE_CSHARP } from '../../shared/languages';
import { TARGET_IL } from '../../shared/targets';
import { saveStateToUrl } from './handlers/url';
import { saveState } from './state';

jest.mock('./handlers/url');
jest.mock('../result-cache/cacheLogic');
const mockedSaveStateToUrl = saveStateToUrl as jest.MockedFunction<typeof saveStateToUrl>;

test(`saveState does not call saveStateToUrl when called for a second time with the same arguments`, () => {
    mockedSaveStateToUrl.mockReturnValue({});
    const state = [
        ['options', [LANGUAGE_CSHARP, null, TARGET_IL, false]],
        ['code', 'test code'],
        ['gist', null]
    ] as const;

    saveState(state);
    saveState(state);

    expect(mockedSaveStateToUrl).toBeCalledTimes(1);
});