export function setContainerIdFromSetup(containerId: string) {
    process.env.TEST_DOCKER_CONTAINER_ID = containerId;
}
export function getContainerId() {
    return process.env.TEST_DOCKER_CONTAINER_ID;
}