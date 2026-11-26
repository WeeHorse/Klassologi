import { html, nothing, render } from 'lit-html';
import { setupIonic } from './ionic';
import './styles.css';
import { mountActivate } from './pages/activate';
import { mountClass } from './pages/class';
import { mountHome } from './pages/home';
import { mountLogin } from './pages/login';
import { mountReset } from './pages/reset';
import { matchRoute, startRouter } from './router';
import { loadStatus, statusView } from './status/status';
import { page } from './ui';

setupIonic();

const root = document.querySelector<HTMLElement>('#page');
if (!root) throw new Error('Elementet #page saknas i index.html.');

// Varje vybyte får ett nytt nummer, så att svar som kommer efter att användaren har lämnat vyn ignoreras.
let generation = 0;

startRouter(() => {
  const current = ++generation;
  const alive = () => current === generation;
  render(nothing, root);
  const route = matchRoute(location.pathname);
  switch (route.name) {
    case 'home':
      return void mountHome(root, alive);
    case 'login':
      return void mountLogin(root, alive);
    case 'activate':
      return void mountActivate(root, alive);
    case 'reset':
      return mountReset(root, alive);
    case 'class':
      return void mountClass(root, alive, route.classId);
    case 'status':
      render(page('Status', statusView({ state: 'loading' })), root);
      return void loadStatus().then((s) => alive() && render(page('Status', statusView(s)), root));
    case 'notFound':
      return render(page('Sidan finns inte', html`<p>Sidan finns inte.</p><p><a href="/">Till startsidan</a></p>`), root);
  }
});
