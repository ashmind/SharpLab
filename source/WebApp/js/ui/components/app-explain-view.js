import Vue from 'vue';
import '../mixins/markdown.js';

Vue.component('app-explain-view', {
    props: {
        explanations: Array
    },
    methods: {
        a(string) {
            if (/\s.*ing$/.test(string))
                return '';
            return /^[aeiou]/.test(string) ? 'an' : 'a';
        }
    },
    template: `<div class="result-content explanations">
        <small class="output-disclaimer">
            This is a new feature — some things might be left unexplained.
        </small>
        <dl>
            <template v-for="explanation in explanations">
                <dt>
                    <code>{{explanation.code.trim()}}</code> is {{a(explanation.name)}} <dfn>{{explanation.name}}</dfn>.
                    <span class="explanation-doc-link">[<a v-bind:href="explanation.link" target="_blank">Docs</a>]</span>
                </dt>
                <dd v-html="markdown(explanation.text)" class="markdown">
                </dd>
            </template>
        </dl>
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});