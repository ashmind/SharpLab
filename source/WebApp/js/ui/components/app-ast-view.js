import Vue from 'vue';
import AstViewItem from './internal/app-ast-view-item.js';

Vue.component('app-ast-view', {
    props: {
        roots: Array
    },
    mounted: function() {
        const getItem = li => this.allById[li.getAttribute('data-id')];

        let hoverSupported = false;
        let lastHoverByClick = null;
        Vue.nextTick(() => {
            handleOnLI(this.$el, 'click', li => {
                li.classList.toggle('collapsed');
                if (hoverSupported)
                    return;

                if (lastHoverByClick)
                    lastHoverByClick.classList.remove('hover');
                this.$emit('item-hover', getItem(li));
                li.classList.add('hover');
                lastHoverByClick = li;
            });
            handleOnLI(this.$el, 'mouseover', li => {
                hoverSupported = true;
                this.$emit('item-hover', getItem(li));
                li.classList.add('hover');
            });
            handleOnLI(this.$el, 'mouseout', li => {
                this.$emit('item-hover', null);
                li.classList.remove('hover');
            });
        });
    },
    render: function(h) {
        this.allById = {};
        return h('div', [renderTree(h, this.roots, this.allById)]);
    }
});

function renderTree(h, items, allById, parentId) {
    return h('ol',
        items.map((item, index) => renderLI(h, item, allById, (parentId != null) ? parentId + '.' + index : index))
    );
}

function renderLI(h, item, allById, id) {
    allById[id] = item;
    return h('li',
        {
            class: { collapsed: true, leaf: !item.children },
            attrs: { 'data-id': id }
        },
        [
            h(AstViewItem, { props: { item } }),
            item.children ? renderTree(h, item.children, allById, id) : null
        ]
    );
}

function handleOnLI(root, event, action) {
    root.addEventListener(event, e => {
        const li = findLI(e);
        if (!li)
            return;
        action(li);
    });
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