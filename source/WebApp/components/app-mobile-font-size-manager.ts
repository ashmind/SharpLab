import Vue from 'vue';
import { uid } from '../ts/ui/helpers/uid';
import { mobileFontSize, setMobileFontSize, MobileFontSize } from './state/mobile-font-size';

export default Vue.component('app-mobile-font-size-manager', {
    data: () => ({
        size: mobileFontSize,
        id: uid()
    }),
    computed: {
        sizeLabel() {
            return ({
                default: 'M',
                large: 'L'
            } as const)[this.size.value];
        }
    },
    mounted() {
        updateBodyClass(this.size.value);
    },
    methods: {
        toggle() {
            const next = this.size.value === 'default' ? 'large' : 'default';
            setMobileFontSize(next);
            updateBodyClass(this.size.value);
        }
    },
    template: '#app-mobile-font-size-manager'
});

function updateBodyClass(size: MobileFontSize) {
    document.body.classList.toggle(`mobile-font-size-large`, size === 'large');
}