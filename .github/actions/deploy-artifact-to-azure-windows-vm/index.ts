import * as core from '@actions/core';
// TODO: replace internal use with direct REST access
import { DownloadHttpClient } from '@actions/artifact/lib/internal/download-http-client';
import { getRuntimeToken } from '@actions/artifact/lib/internal/config-variables';
import { ComputeManagementClient } from '@azure/arm-compute';
import { TokenCredentials } from '@azure/ms-rest-js';
import { AzureCliCredential } from '@azure/identity';

// eslint-disable-next-line @typescript-eslint/no-floating-promises
(async () => {
    try {
        const azureSubscriptionId = core.getInput('azure-subscription', { required: true });
        const azureResourceGroupName = core.getInput('azure-resource-group', { required: true });
        const azureVMName = core.getInput('azure-vm', { required: true });
        const artifactName = core.getInput('artifact-name', { required: true });
        const artifactDownloadPath = core.getInput('artifact-download-path', { required: true });
        const deployScript = core.getInput('deploy-script-inline', { required: true });

        const artifactUrl = process.env.LOCAL_TEST_ARTIFACT_URL ?? await getArtifactUrl(artifactName);
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

    const files = (await client.getContainerItems(artifact.name, artifact.fileContainerResourceUrl))
        .value.filter(i => i.itemType === 'file');
    if (files.length !== 1)
        throw new Error(`Artifact '${name}' has ${files.length} files. Only 1 file per artifact is currently supported.`);

    return files[0].contentLocation;
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

    console.log('DEBUG: computeClient.virtualMachines.runCommand');
    const result = (await computeClient.virtualMachines.runCommand(azureResourceGroupName, azureVMName, {
        commandId: 'RunPowerShellScript',
        script: [
            'param ([string] $ArtifactUrl, [string] $ArtifactUrlToken, [string] $ArtifactDownloadPath)',
            "$ErrorActionPreference = 'Stop'",
            'Invoke-RestMethod $ArtifactUrl -OutFile $ArtifactDownloadPath',
            deployScript
        ],
        parameters: [
            { name: 'ArtifactUrl', value: artifactUrl },
            { name: 'ArtifactUrlToken', value: getRuntimeToken() },
            { name: 'ArtifactDownloadPath', value: artifactDownloadPath }
        ]
    })) as unknown as {
        properties: { output: { value: [{
            code: 'ComponentStatus/StdOut/succeeded'|'ComponentStatus/StdErr/succeeded',
            message: string
        } ] } }
    };

    let error = null;
    for (const { code, message } of result.properties.output.value) {
        if (!message)
            continue;

        if (code === 'ComponentStatus/StdErr/succeeded') {
            console.error(`[VM] ${message}`);
            error = message;
            continue;
        }

        console.log(`[VM] ${message}`);
    }

    if (error)
        throw new Error(`Deploy script reported error from the VM: ${error}`);
}