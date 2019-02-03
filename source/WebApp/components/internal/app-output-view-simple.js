export default {
    props: {
        inspection: Object
    },
    computed: {
        isMultiline() {
            return /[\r\n]/.test(this.inspection.value);
        },

        isException() {
            return this.inspection.title === 'Exception';
        },

        isWarning() {
            return this.inspection.title === 'Warning';
        }
    },
    template: '#app-output-view-simple'
};