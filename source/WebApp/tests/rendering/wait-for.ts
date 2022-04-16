export async function waitFor(ready: () => Promise<boolean>|boolean, error: () => Error) {
    let remainingRetryCount = 50;
    while (!(await Promise.resolve(ready()))) {
        if (remainingRetryCount === 0)
            throw error();
        await new Promise(resolve => setTimeout(resolve, 100));
        remainingRetryCount -= 1;
    }
}