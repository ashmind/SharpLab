export default {
    props: {
        item: {}
    },
    methods: {
        renderValue
    },
    template: '#app-ast-view-item'
};

function renderValue(value, type) {
    if (type === 'trivia')
        return escapeTrivia(value);

    return escapeCommon(value);
}

function escapeCommon(value) {
    return value
        .replace('\r', '\\r')
        .replace('\n', '\\n')
        .replace('\t', '\\t');
}

function escapeTrivia(value) {
    return escapeCommon(value)
        .replace(/(^ +| +$)/g, (_,$1) => $1.length > 1 ? `<space:${$1.length}>` : '<space>');
}