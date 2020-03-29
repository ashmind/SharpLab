/* globals appInsights:false */
export default window.appInsights
    ? ((feature: string) => appInsights.trackEvent(feature))
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    : (() => {});