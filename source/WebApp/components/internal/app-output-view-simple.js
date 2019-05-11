export default {
    props: {
        inspection: Object
    },
    computed: {
        isMultiline() {
            return this.inspection.value && /[\r\n]/.test(this.inspection.value);
        },

        isException() {
            return this.inspection.title === 'Exception';
        },

        isWarning() {
            return this.inspection.title === 'Warning';
        },

        isTitleOnly() {
            return this.inspection.value === undefined;
        }
    },
    template: '#app-output-view-simple'
};