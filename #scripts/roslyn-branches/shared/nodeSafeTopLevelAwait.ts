export const nodeSafeTopLevelAwait = (
    call: () => Promise<unknown>,
    handleError: (e: unknown) => void,
    { timeoutMinutes }: { timeoutMinutes: number }
) => {
    let keepaliveTimer: ReturnType<typeof setTimeout>;
    // https://github.com/nodejs/node/issues/22088
    const keepalive = () => new Promise<void>((_, reject) => keepaliveTimer = setTimeout(
        () => reject(new Error(`Top-level async timed out within ${timeoutMinutes} minutes.`)), timeoutMinutes * 60 * 1000
    ));

    // eslint-disable-next-line @typescript-eslint/no-floating-promises
    (async () => {
        try {
            await Promise.race([call(), keepalive()]);
        }
        catch (e) {
            handleError(e);
        }
        finally {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            clearTimeout(keepaliveTimer!);
        }
    })();
};