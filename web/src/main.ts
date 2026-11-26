import { render } from 'lit-html';
import { loadStatus, statusView } from './status/status';

const root = document.querySelector<HTMLElement>('#app');
if (!root) throw new Error('Elementet #app saknas i index.html.');

render(statusView({ state: 'loading' }), root);
loadStatus().then((status) => render(statusView(status), root));
