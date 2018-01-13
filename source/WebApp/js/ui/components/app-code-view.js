import Vue from 'vue';
import CodeMirror from 'codemirror';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';
import '../codemirror/mode-cil.js';
import '../codemirror/mode-asm.js';
import '../codemirror/addon-infotip.js';
import '../codemirror/addon-cil-infotip.js';
import targets from '../../helpers/targets.js';

Vue.component('app-code-view', {
    props: {
        value:    String,
        language: String
    },
    mounted: function() {
        Vue.nextTick(() => {
            const modeMap = {
                [targets.csharp]: 'text/x-csharp',
                [targets.vb]:     'text/x-vb',
                [targets.il]:     'text/x-cil',
                [targets.asm]:    'text/x-asm'
            };

            const textarea = this.$el.firstChild;
            textarea.value = this.value;
            const options = {
                readOnly: true,
                indentUnit: 4,
                mode: modeMap[this.language],
                infotip: {}
            };
            const cm = CodeMirror.fromTextArea(textarea, options);
            this.cm = cm;

            const wrapper = cm.getWrapperElement();
            wrapper.classList.add('mirrorsharp-theme');

            const codeElement = wrapper.getElementsByClassName('CodeMirror-code')[0];
            if (codeElement && codeElement.contentEditable) // HACK, mobile only
                codeElement.contentEditable = false;

            this.$watch('language', value => {
                cm.setOption('mode', modeMap[value]);
            });
            this.$watch('value', value => {
                value = value != null ? value : '';
                if (cm.getValue() === value)
                    return;
                cm.setValue(value);
            });
        });
    },

    beforeDestroy: function() {
        this.cm.toTextArea();
    },

    template: '<div><textarea></textarea></div>'
});