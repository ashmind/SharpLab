import Vue from 'vue';
import CodeMirror from 'codemirror';
import 'codemirror/addon/lint/lint';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';


function buildGetAnnotations(data) {
    return (cm, updateLinting) => {
        data.lint(data.value, updateLinting);
    };
}

Vue.component('app-codemirror', {
    props: {
        value:   String,
        mode:    String,
        lint:    Function,
        options: Object
    },
    ready: function() {
        const textarea = this.$el;
        textarea.value = this.value;                
        const options = Object.assign(
            {},
            this.options,
            this.mode !== undefined ? { mode: this.mode } : {},
            this.lint !== undefined ? {
                lint: { async: true, getAnnotations: buildGetAnnotations(this) }
            } : {}
        );
        const instance = CodeMirror.fromTextArea(textarea, options);
        this.$watch('mode', value => instance.setOption('mode', value));
        
        let settingValue = false;
        this.$watch('value', value => {
            value = value != null ? value : '';
            if (instance.getValue() === value)
                return;

            settingValue = true;
            instance.setValue(value);
            settingValue = false;
        });

        instance.on('change', () => {
            if (settingValue)
                return;

            this.value = instance.getValue();
        });
    },
    template: '<textarea></textarea>'
});