import Vue from 'vue';

Vue.directive('app-class-toggle', {
    inserted: (el, binding) => {
        const options = binding.value;
        const target = document.querySelector(options.target);
        el.addEventListener('click', () => {
            target.classList.toggle(options.class);
        });
    }
});