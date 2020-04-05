// eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
export default window.appInsights
    ? ((feature: string) => { window.appInsights.trackEvent(feature); })
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    : (() => {});