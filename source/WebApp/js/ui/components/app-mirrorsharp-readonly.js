import Vue from 'vue';
import targets from '../../helpers/targets.js';
import CodeMirror from 'codemirror';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';
import '../codemirror/mode-cil.js';
import '../codemirror/mode-asm.js';

Vue.component('app-mirrorsharp-readonly', {
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
                mode: modeMap[this.language]
            };
            const cm = CodeMirror.fromTextArea(textarea, options);
            this.cm = cm;

            const wrapper = cm.getWrapperElement();
            wrapper.classList.add('mirrorsharp-theme');

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