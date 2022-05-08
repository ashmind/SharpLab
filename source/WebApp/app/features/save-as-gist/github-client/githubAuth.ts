// This value is set by page generated at /github/auth/complete
// (by server-side middleware)
const token = localStorage['sharplab.github.token'] as string|undefined;

let startingRedirect = false;

const redirectingKey = 'sharplab.github.auth.redirecting';
// Includes token check: if auth didn't work, we should not repeat
// the pre-redirect action and get into a redirect again.
const isBackFromRedirect = token && sessionStorage[redirectingKey] === 'true';
sessionStorage.removeItem(redirectingKey);

export { token };

export const githubAuth = Object.freeze({
    redirectIfRequiredAsync() {
        if (token)
            return Promise.resolve();

        sessionStorage[redirectingKey] = 'true';
        // This value is loaded by page generated at /github/auth/complete
        // (by server-side middleware)
        sessionStorage['sharplab.github.auth.return'] = window.location.href;
        window.location.href = '/github/auth/start';
        // Never resolved, other code should not run since the page is going
        // to be redirected anyways. We just need to wait a bit for redirect to happen.
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        return new Promise(() => {});
    },

    redirectIfRequired() {
        if (token)
            return false;

        if (startingRedirect)
            return 'redirecting';

        startingRedirect = true;
        sessionStorage[redirectingKey] = 'true';
        // This value is loaded by page generated at /github/auth/complete
        // (by server-side middleware)
        sessionStorage['sharplab.github.auth.return'] = window.location.href;
        window.location.href = '/github/auth/start';
        return 'redirecting';
    },

    isBackFromRedirect
});