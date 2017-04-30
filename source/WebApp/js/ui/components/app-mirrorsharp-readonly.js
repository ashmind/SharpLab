import Vue from 'vue';
import languages from 'helpers/languages';
import CodeMirror from 'codemirror';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';
import 'ui/codemirror/mode-asm';

Vue.component('app-mirrorsharp-readonly', {
    props: {
        value:    String,
        language: String
    },
    ready: function() {
        const modeMap = {
            [languages.csharp]: 'text/x-csharp',
            [languages.vb]:     'text/x-vb',
            [languages.il]:     '',
            [languages.asm]:    'text/x-asm'
        };

        const textarea = this.$el;
        textarea.value = this.value;
        const options = {
            readOnly: true,
            indentUnit: 4,
            mode: modeMap[this.language]
        };
        const instance = CodeMirror.fromTextArea(textarea, options);
        const wrapper = instance.getWrapperElement();
        wrapper.classList.add('mirrorsharp-theme');

        this.$watch('language', value => instance.setOption('mode', modeMap[value]));

        this.$watch('value', value => {
            value = value != null ? value : '';
            if (instance.getValue() === value)
                return;
            instance.setValue(value);
        });
    },
    template: '<textarea></textarea>'
});