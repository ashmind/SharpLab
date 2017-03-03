/*
npm install text-to-svg && npm install svgo
*/
const SVGO = require('svgo');
const TextToSVG = require('text-to-svg');

const svgo = new SVGO();
const text = TextToSVG
  .loadSync(`${process.env.WINDIR}\\Fonts\\consola.ttf`)
  .getSVG('?.', { x: 7, y: 105, fontSize: 90, attributes: { fill: '#fff' } })
  .replace(/<\/?svg[^>]*>/g, '');
const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128">
  <rect fill="#4684ee" stroke="#fff" width="100%" height="100%"></rect>
  ${text}
</svg>`

svgo.optimize(svg, optimized => console.log(optimized.data));