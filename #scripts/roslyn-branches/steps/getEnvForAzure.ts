export default function getEnvForAzure(name: string) {
    const value = process.env[name];
    if (!value)
        throw `Environment variable ${name} is required for Azure deployment.`;
    return value;
}