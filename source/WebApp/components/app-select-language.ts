import Vue from 'vue';
import { languages, LanguageName } from '../ts/helpers/languages';
import './app-select';

export default Vue.component('app-select-language', {
    props: {
        value: String as () => LanguageName,
        useAriaLabel: {
            default: true,
            type: Boolean
        }
    },

    data() {
        return {
            language: this.value,
            languages
        };
    },

    watch: {
        value() {
            this.language = this.value;
        },

        language() {
            this.$emit('input', this.language);
        }
    },

    inheritAttrs: false,

    template: '#app-select-language'
});