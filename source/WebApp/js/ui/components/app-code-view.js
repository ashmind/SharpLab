import Vue from 'vue';
import CodeMirror from 'codemirror';
import debounce from 'throttle-debounce/debounce';
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
        language: String,
        ranges:   Array
    },
    methods: {
        hasRanges() {
            return (this.ranges && this.ranges.length > 0);
        },

        hover(x, y) {
            if (!this.hasRanges())
                return;
            if (this.highlighted.by === 'cursor')
                return;
            this.highlightAt(this.cm.coordsChar({ left: x, top: y }), 'hover');
        },

        highlightAt(location, by) {
            if (!this.hasRanges())
                return;
            const range = this.ranges.find(r => this.isLocationBetween(location, r.result.start, r.result.end));
            if (this.highlighted.range === range) {
                this.highlighted.by = by;
                return;
            }
            if (this.highlighted.marker) {
                this.highlighted.marker.clear();
                this.highlighted = {};
            }
            this.$emit('range-active', range);
            if (!range)
                return;
            this.highlighted = {
                by,
                marker: this.cm.markText(range.result.start, range.result.end, { className: 'highlighted' }),
                range
            };
        },

        isLocationBetween(location, start, end) {
            if (location.line < start.line)
                return false;
            if (location.line === start.line && location.ch < start.ch)
                return false;
            if (location.line > end.line)
                return false;
            if (location.line === end.line && location.ch > end.ch)
                return false;
            return true;
        }
    },
    created() {
        this.delayedHover = debounce(50, false, this.hover.bind(this));
        this.highlighted = {};
    },
    async mounted() {
        await Vue.nextTick();

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

        cm.on('cursorActivity', () => this.highlightAt(cm.getCursor(), 'cursor'));
        CodeMirror.on(wrapper, 'mousemove', e => this.delayedHover(e.pageX, e.pageY));

        this.$watch('language', value => {
            cm.setOption('mode', modeMap[value]);
        });
        this.$watch('value', value => {
            value = value != null ? value : '';
            if (cm.getValue() === value)
                return;
            cm.setValue(value);
        });
    },

    beforeDestroy() {
        this.cm.toTextArea();
    },

    template: '<div><textarea></textarea></div>'
});