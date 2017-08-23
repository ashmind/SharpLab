/* globals appInsights:false */
export default window.appInsights
    ? (feature => appInsights.trackEvent(feature))
    : (() => {});