import * as msRestNodeAuth from '@azure/ms-rest-nodeauth';

const noAudience = Symbol('no-audience') as unknown as 'symbol:no-audience';

function getEnvForAzure(name: string) {
    const value = process.env[name];
    if (!value)
        throw `Environment variable ${name} is required for Azure deployment.`;
    return value;
}

async function loginToAzure(tokenAudience?: string) {
    const appId = getEnvForAzure('SL_BUILD_AZURE_APP_ID');
    const secret = getEnvForAzure('SL_BUILD_AZURE_SECRET');
    const tenantId = getEnvForAzure('SL_BUILD_AZURE_TENANT');

    console.log('Logging in to Azure...');
    const { credentials, subscriptions } = await msRestNodeAuth.loginWithServicePrincipalSecretWithAuthResponse(appId, secret, tenantId, {
        ...(tokenAudience ? { tokenAudience } : {})
    });
    if (tokenAudience)
        return { credentials };

    if (!subscriptions || subscriptions.length !== 1)
        throw new Error(`Expected single Azure subscription, but got ${subscriptions?.length ?? '<null>'}.`);

    return { credentials, subscriptionId: subscriptions[0].id };
}

type UnwrapPromise<T> = T extends PromiseLike<infer U> ? U : T;
const cached = {} as Record<string, UnwrapPromise<ReturnType<typeof loginToAzure>>|undefined>;

async function getAzureCredentialsInternal(audience?: string) {
    let result = cached[audience ?? noAudience];
    if (!result) {
        result = await loginToAzure(audience);
        cached[audience ?? noAudience] = result;
    }

    return result;
}

export const getAzureCredentials = () => getAzureCredentialsInternal() as Promise<{
    credentials: msRestNodeAuth.TokenCredentialsBase;
    subscriptionId: string;
}>;
export const getAzureCredentialsForAudience = async (audience: string) => (await getAzureCredentialsInternal(audience)).credentials;