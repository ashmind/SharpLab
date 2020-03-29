import Vue from 'vue';
import { debounce } from 'throttle-debounce';
import CodeMirror from 'codemirror';
import 'codemirror/mode/clike/clike';
import 'codemirror/mode/vb/vb';
import './internal/codemirror/mode-cil';
import './internal/codemirror/mode-asm';
import './internal/codemirror/addon-cil-infotip';
import type { CodeRange } from '../ts/types/code-range';
import { targets } from '../ts/helpers/targets';
import extendType from '../ts/helpers/extend-type';

type TargetLanguageName = typeof targets.csharp|typeof targets.vb|typeof targets.il|typeof targets.asm;

export default Vue.component('app-code-view', {
    props: {
        value:    String,
        language: String as () => TargetLanguageName,
        ranges:   Array as () => ReadonlyArray<{
            readonly source: CodeRange;
            readonly result: CodeRange;
        }>
    },
    data: () => extendType({})<{
        cm: CodeMirror.Editor;
        highlighted: {
            by: 'cursor'|'hover';
            marker: CodeMirror.TextMarker;
            range: {
                source: CodeRange;
                result: CodeRange;
            };
        }|{
            by?: undefined;
            marker?: undefined;
            range?: undefined;
        };
        delayedHover: (x: number, y: number) => void;
    }>(),
    methods: {
        hasRanges() {
            return (this.ranges && this.ranges.length > 0);
        },

        hover(x: number, y: number) {
            if (!this.hasRanges())
                return;
            if (this.highlighted.by === 'cursor')
                return;
            this.highlightAt(this.cm.coordsChar({ left: x, top: y }), 'hover');
        },

        highlightAt(location: CodeMirror.Position, by: 'cursor'|'hover') {
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

        isLocationBetween(location: CodeMirror.Position, start: CodeMirror.Position, end: CodeMirror.Position) {
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

        const textarea = this.$el.firstChild as HTMLTextAreaElement;
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

        const codeElement = wrapper.getElementsByClassName('CodeMirror-code')[0] as unknown as ElementContentEditable;
        if (codeElement && codeElement.contentEditable) { // HACK, mobile only
            codeElement.contentEditable = false as unknown as string;
        }

        cm.on('cursorActivity', () => this.highlightAt(cm.getCursor(), 'cursor'));
        CodeMirror.on(wrapper, 'mousemove', (e: MouseEvent) => this.delayedHover(e.pageX, e.pageY));

        this.$watch('language', (value: TargetLanguageName) => {
            cm.setOption('mode', modeMap[value]);
        });
        this.$watch('value', (value: string) => {
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