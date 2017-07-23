export default Object.freeze(
    JSON.parse(localStorage['sharplab.features'] || 'null') || {}
);