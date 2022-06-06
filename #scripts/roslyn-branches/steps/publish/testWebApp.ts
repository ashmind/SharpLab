import delay from 'delay';
import safeFetch, { Response, SafeFetchError } from '../../helpers/safeFetch';

export const testWebApp = async ({ url }: { url: string }) => {
    console.log(`GET ${url}/status`);
    let ok = false;
    let tryPermanent = 1;
    let tryTemporary = 1;

    const formatStatus = ({ status, statusText }: Pick<Response, 'status'|'statusText'>) =>
        `  ${status} ${statusText}`;

    while (tryPermanent < 3 && tryTemporary < 30) {
        try {
            const response = await safeFetch(`${url}/status`);
            ok = true;
            console.log(formatStatus(response));
            break;
        }
        catch (e) {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
            if ((e as { response?: Response }).response) {
                // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
                console.warn(formatStatus((e as { response: Response }).response));
            }

            const { status } = (e as Partial<SafeFetchError>).response ?? {};
            const temporary = status === 503 || status === 403;
            if (temporary) {
                tryTemporary += 1;
            }
            else {
                tryPermanent += 1;
            }
            console.warn(e);
        }
        await delay(1000);
    }

    if (!ok)
        throw new Error(`Failed to get success from ${url}/status`);
};