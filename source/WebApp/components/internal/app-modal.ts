import Vue from 'vue';
import extendType from '../../ts/helpers/extend-type';
import '../../ts/ui/setup/portal-vue';

export default Vue.component('app-modal', {
    props: {
        title: String,
        canClose: {
            type: Boolean,
            default: true
        }
    },
    data: () => extendType({
        wasOpen: false,
        isOpen: false
    })<{
        escListener: (e: KeyboardEvent) => void;
    }>(),
    methods: {
        open() {
            this.escListener = e => {
                if (e.key === 'Escape')
                    this.close();
            };
            this.wasOpen = true;
            this.isOpen = true;
            document.addEventListener('keyup', this.escListener);
        },

        close() {
            this.isOpen = false;
            document.removeEventListener('keyup', this.escListener);
            this.$emit('close');
        }
    },
    template: '#app-modal'
});