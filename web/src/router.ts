/** Minimal router över History API:t. Varje vy är en modul som renderar sig själv med lit-html. */

export type Route =
  | { name: 'home' }
  | { name: 'login' }
  | { name: 'activate' }
  | { name: 'reset' }
  | { name: 'class'; classId: string }
  | { name: 'status' }
  | { name: 'notFound' };

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function matchRoute(pathname: string): Route {
  const path = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname;
  switch (path) {
    case '/':
      return { name: 'home' };
    case '/logga-in':
      return { name: 'login' };
    case '/aktivera':
      return { name: 'activate' };
    case '/aterstall':
      return { name: 'reset' };
    case '/status':
      return { name: 'status' };
  }
  const classMatch = /^\/klasser\/([^/]+)$/.exec(path);
  if (classMatch && uuid.test(classMatch[1])) return { name: 'class', classId: classMatch[1] };
  return { name: 'notFound' };
}

let onChange: () => void = () => {};

export function startRouter(render: () => void): void {
  onChange = render;
  window.addEventListener('popstate', () => onChange());
  // Interna länkar, även inuti Ionics Shadow DOM, hanteras utan omladdning.
  document.addEventListener('click', (event) => {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    const anchor = event.composedPath().find((el): el is HTMLAnchorElement => el instanceof HTMLAnchorElement);
    if (!anchor || anchor.target || anchor.origin !== location.origin) return;
    event.preventDefault();
    navigate(anchor.pathname + anchor.search + anchor.hash);
  });
  onChange();
}

export function navigate(path: string, options: { replace?: boolean } = {}): void {
  if (options.replace) history.replaceState(null, '', path);
  else history.pushState(null, '', path);
  onChange();
}
