import Vue from 'vue';

function renderValue(value: null|string|unknown, type: 'trivia'|unknown) {
    if (value === null)
        return 'null';

    if (typeof value !== 'string')
        return value;

    if (type === 'trivia')
        return escapeTrivia(value);

    return escapeCommon(value);
}

function escapeCommon(value: string) {
    return value
        .replace('\r', '\\r')
        .replace('\n', '\\n')
        .replace('\t', '\\t');
}

function escapeTrivia(value: string) {
    return escapeCommon(value)
        .replace(/(^ +| +$)/g, (_,$1) => $1.length > 1 ? `<space:${$1.length}>` : '<space>');
}

export default Vue.extend({
    props: {
        item: {}
    },
    methods: {
        renderValue
    },
    template: '#app-ast-view-item'
});