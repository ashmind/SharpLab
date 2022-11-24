import { SubscriptionClient } from '@azure/arm-subscriptions';
import { ClientSecretCredential, TokenCredential } from '@azure/identity';

let cachedCredential: TokenCredential | undefined;
let cachedSubscriptionId: string | undefined;

function getEnvForAzure(name: string) {
    const value = process.env[name];
    if (!value)
        throw `Environment variable ${name} is required for Azure deployment.`;
    return value;
}

export const getAzureCredential = () => {
    if (!cachedCredential) {
        console.log('Configuring Azure credential...');
        const appId = getEnvForAzure('SL_BUILD_AZURE_APP_ID');
        const secret = getEnvForAzure('SL_BUILD_AZURE_SECRET');
        const tenantId = getEnvForAzure('SL_BUILD_AZURE_TENANT');

        cachedCredential = new ClientSecretCredential(tenantId, appId, secret);
    }
    return cachedCredential;
};

export const getAzureCredentialWithSubscriptionId = async () => {
    const credential = getAzureCredential();
    if (!cachedSubscriptionId) {
        console.log('Getting Azure subscriptions...');
        const subscriptions = new SubscriptionClient(credential).subscriptions.list();
        let subscriptionId: string | undefined;
        for await (const subscription of subscriptions) {
            if (subscriptionId)
                throw new Error(`Expected single Azure subscription, but got multiple.`);
            ({ subscriptionId } = subscription);
        }
        if (!subscriptionId)
            throw new Error(`Expected single Azure subscription, but got none.`);
        cachedSubscriptionId = subscriptionId;
    }

    return { credential, subscriptionId: cachedSubscriptionId };
};