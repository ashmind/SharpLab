import Vue from 'vue';
import AstViewItem from './internal/app-ast-view-item.js';

Vue.component('app-ast-view', {
    props: {
        roots: Array
    },
    methods: {
        expand(item, li = null) {
            if (!item.children || this.isExpanded(item))
                return;
            this.expanded.add(item);
            li = li || getItemLI(item, this);
            li.classList.remove('collapsed');
        },

        collapse(item, li = null) {
            if (!item.children || !this.isExpanded(item))
                return;
            this.expanded.delete(item);
            li = li || getItemLI(item, this);
            li.classList.add('collapsed');
        },

        isExpanded(item) {
            return this.expanded.has(item);
        },

        select(item, li = null) {
            if (this.selected.item === item)
                return;
            if (this.selected.li)
                this.selected.li.classList.remove('selected');
            this.$emit('item-select', item);
            if (item) {
                li = li || getItemLI(item, this);
                li.classList.add('selected');
            }
            this.selected = { li, item };
        },

        selectDeepestByOffset(offset) {
            const recurse = items => {
                for (const item of items) {
                    if (!matchesOffset(item, offset))
                        continue;

                    if (item.children) {
                        this.expand(item);
                        recurse(item.children);
                        break;
                    }

                    const li = getItemLI(item, this);
                    this.select(item, li);
                    li.scrollIntoView();
                    break;
                }
            };
            recurse(this.roots);
        }
    },
    created() {
        this.itemsById = {};
        this.ids = new WeakMap();
        this.expanded = new WeakSet();
        this.selected = { item: null, li: null };
    },
    async mounted() {
        await Vue.nextTick();

        const getItem = li => this.itemsById[li.getAttribute('data-id')];
        let hoverDetected = false;

        handleOnLI(this.$el, 'click', li => {
            const item = getItem(li);
            if (!this.isExpanded(item)) {
                this.expand(item, li);
            }
            else {
                this.collapse(item, li);
            }

            // select-on-click is only enabled in mobile and such
            if (hoverDetected)
                return;
            this.select(item, li);
        });
        handleOnLI(this.$el, 'mouseover', li => {
            hoverDetected = true;
            this.select(getItem(li), li);
        });
        handleOnLI(this.$el, 'mouseout', () => {
            this.select(null);
        });
    },
    render(h) {
        this.itemsById = {};
        return h('div', [renderTree(h, this.roots, this)]);
    }
});

function renderTree(h, items, that, parentId) {
    return h('ol',
        items.map((item, index) => renderLI(h, item, that, (parentId != null) ? parentId + '.' + index : index))
    );
}

function renderLI(h, item, that, id) {
    that.itemsById[id] = item;
    that.ids.set(item, id);
    return h('li',
        {
            class: { collapsed: !that.isExpanded(item), leaf: !item.children },
            attrs: { 'data-id': id }
        },
        [
            h(AstViewItem, { props: { item } }),
            item.children ? renderTree(h, item.children, that, id) : null
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

function getItemLI(item, that) {
    const id = that.ids.get(item);
    return that.$el.querySelector(`li[data-id='${id}']`);
}

function matchesOffset(item, offset) {
    if (!item.range)
        return false;
    const [start, end] = item.range.split('-');
    return offset >= parseInt(start)
        && offset <= parseInt(end);
}