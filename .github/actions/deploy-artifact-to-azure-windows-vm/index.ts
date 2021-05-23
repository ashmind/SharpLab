import * as core from '@actions/core';
// TODO: replace internal use with direct REST access
import { DownloadHttpClient } from '@actions/artifact/lib/internal/download-http-client';
import { ComputeManagementClient } from '@azure/arm-compute';
import { TokenCredentials } from '@azure/ms-rest-js';
import { AzureCliCredential } from '@azure/identity';

// eslint-disable-next-line @typescript-eslint/no-floating-promises
(async () => {
    try {
        const azureSubscriptionId = core.getInput('azure-subscription');
        const azureResourceGroupName = core.getInput('azure-resource-group');
        const azureVMName = core.getInput('azure-vm-name');
        const artifactName = core.getInput('artifact-name');
        const artifactDownloadPath = core.getInput('artifact-download-path');
        const deployScript = core.getInput('deploy-script-inline');

        const artifactUrl = await getArtifactUrl(artifactName);
        console.log(`Artifact URL: ${artifactUrl}`);

        await uploadArtifactAndRunDeploy({
            azureSubscriptionId,
            azureResourceGroupName,
            azureVMName,
            artifactUrl,
            artifactDownloadPath,
            deployScript
        });
    }
    catch (e) {
        core.setFailed(e);
    }
})();

async function getArtifactUrl(name: string) {
    const client = new DownloadHttpClient();
    const artifact = (await client.listArtifacts()).value.find(a => a.name === name);
    if (!artifact)
        throw new Error(`Artifact '${name}' was not found.`);

    return artifact.url;
}

async function uploadArtifactAndRunDeploy({
    azureSubscriptionId,
    azureResourceGroupName,
    azureVMName,
    artifactUrl,
    artifactDownloadPath,
    deployScript
}: {
    azureSubscriptionId: string,
    azureResourceGroupName: string,
    azureVMName: string,
    artifactUrl: string,
    artifactDownloadPath: string,
    deployScript: string
}) {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const token = (await new AzureCliCredential().getToken('https://management.azure.com/.default'))!.token;
    const computeClient = new ComputeManagementClient(new TokenCredentials(token), azureSubscriptionId);

    const result = await computeClient.virtualMachines.runCommand(azureResourceGroupName, azureVMName, {
        commandId: 'RunPowerShellScript',
        script: [
            "$ErrorActionPreference = 'Stop'",
            `Invoke-RestMethod $ArtifactUrl -OutFile $ArtifactDownloadPath`,
            deployScript
        ],
        parameters: [
            { name: 'ArtifactUrl', value: artifactUrl },
            { name: 'ArtifactDownloadPath', value: artifactDownloadPath }
        ]
    });

    console.log(JSON.stringify(result));
}