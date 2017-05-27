import Vue from 'vue';

Vue.component('app-ast-view', {
    props: {
        roots: Array
    },
    methods: {
        renderValue: function(value, type) {
            if (type === 'trivia')
                return escapeTrivia(value);

            return escapeCommon(value);
        }
    },
    data: () => ({
        expanded: []
    }),
    mounted: function() {
        Vue.nextTick(() => {
            this.$el.addEventListener('click', e => {
                const li = findLI(e);
                if (!li)
                    return;
                li.classList.toggle('collapsed');
                e.stopPropagation();
            });
        });
    },
    template: '#app-ast-view'
});

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

function findLI(e) {
    let element = e.target;
    while (element && element.tagName !== 'OL') {
        if (element.tagName === 'LI')
            return element;
        element = element.parentElement;
    }
    return null;
}