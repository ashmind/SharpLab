import Vue from 'vue';

Vue.directive('app-class-toggle', {
    inserted: (el, binding) => {
        const options = binding.value as { target: string; class: string };
        const target = document.querySelector(options.target);
        el.addEventListener('click', () => {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            target!.classList.toggle(options.class);
        });
    }
});