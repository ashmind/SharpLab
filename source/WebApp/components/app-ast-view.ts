import Vue, { CreateElement, VNode } from 'vue';
import type { AstItem } from '../ts/types/results';
import type { AstViewRef } from '../ts/types/component-ref-interfaces/ast-view-ref';
import { assertMatchesRef } from '../ts/helpers/assert-matches-ref';
import extendType from '../ts/helpers/extend-type';
import AstViewItem from './internal/app-ast-view-item';

const AstView = Vue.component('app-ast-view', {
    props: {
        roots: Array as () => ReadonlyArray<AstItem>
    },
    data: () => extendType({})<{
        itemsById: { [id: string]: AstItem };
        expanded: WeakSet<AstItem>;
        ids: WeakMap<AstItem, string>;
        selected: {
            item: AstItem|null;
            li: HTMLLIElement|null;
        };
    }>(),
    methods: {
        expand(item: AstItem, li: HTMLLIElement|null = null) {
            if (!item.children || this.isExpanded(item))
                return;
            this.expanded.add(item);
            li = li || getItemLI(item, this);
            li.classList.remove('collapsed');
        },

        collapse(item: AstItem, li: HTMLLIElement|null = null) {
            if (!item.children || !this.isExpanded(item))
                return;
            this.expanded.delete(item);
            li = li || getItemLI(item, this);
            li.classList.add('collapsed');
        },

        isExpanded(item: AstItem) {
            return this.expanded.has(item);
        },

        select(item: AstItem|null, li: HTMLLIElement|null = null) {
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

        selectDeepestByOffset(offset: number) {
            const recurse = (items: ReadonlyArray<AstItem>) => {
                for (const item of items) {
                    if (!matchesOffset(item, offset))
                        continue;

                    if (item.children) {
                        this.expand(item);
                        if (recurse(item.children))
                            return true;
                    }

                    const li = getItemLI(item, this);
                    this.select(item, li);
                    li.scrollIntoView();
                    return true;
                }
                return false;
            };
            recurse(this.processedRoots);
        }
    },
    computed: {
        processedRoots(): ReadonlyArray<AstItem> {
            return preprocessItems(this.roots);
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

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const getItem = (li: HTMLLIElement) => this.itemsById[li.getAttribute('data-id')!];
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
    render(h): VNode {
        this.itemsById = {};
        return h(
            'div', { class: { ast: true } },
            [renderTree(h, this.processedRoots, this)]
        );
    }
});

assertMatchesRef<AstViewRef, InstanceType<typeof AstView>>();

function preprocessItems(items: ReadonlyArray<AstItem>) {
    return items.map(item => {
        if (typeof item !== 'object') // simple value
            return item;

        const processed = Object.assign({}, item);
        delete processed.properties;

        const childrenFromProperties = Object
            .entries(item.properties || {})
            .map(([name, value]) => ({ type: 'property-only', property: name, value } as AstItem));

        if (childrenFromProperties.length === 0 && !item.children)
            return processed;

        processed.children = childrenFromProperties.concat(preprocessItems(item.children || []));
        return processed;
    });
}

function renderTree(h: CreateElement, items: ReadonlyArray<AstItem>, that: InstanceType<typeof AstView>, parentId?: string): VNode {
    return h('ol',
        items.map((item, index) => renderLI(h, item, that, (parentId != null) ? parentId + '.' + index : index.toString()))
    );
}

function renderLI(h: CreateElement, item: AstItem, that: InstanceType<typeof AstView>, id: string): VNode {
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

function handleOnLI(root: Element, event: keyof GlobalEventHandlersEventMap, action: (li: HTMLLIElement) => void) {
    root.addEventListener(event, e => {
        const li = findLI(e);
        if (!li)
            return;
        action(li);
    });
}

function findLI(e: Event) {
    let element = e.target as HTMLElement|undefined|null;
    while (element && element.tagName !== 'OL') {
        if (element.tagName === 'LI')
            return element as HTMLLIElement;
        element = element.parentElement;
    }
    return null;
}

function getItemLI(item: AstItem, that: InstanceType<typeof AstView>): HTMLLIElement {
    const id = that.ids.get(item);
    return that.$el.querySelector(`li[data-id='${id}']`) as HTMLLIElement;
}

function matchesOffset(item: AstItem, offset: number) {
    if (!item.range)
        return false;
    const [start, end] = item.range.split('-');
    return offset >= parseInt(start)
        && offset <= parseInt(end);
}

export default AstView;