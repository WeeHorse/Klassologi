import { html, nothing, type TemplateResult } from 'lit-html';
import { alertController } from '@ionic/core/components';
import { api, fieldError, problemMessage, type Me, type Post, type Problem, type Role } from '../api';
import { renderMarkdown } from '../markdown';
import { errorText, formatDateTime, inputValue, invalidClass, roleLabel, submitButton, toast } from '../ui';

/** Steg 4: klassens anslagstavla (UC-08–10, fästning). Renderas som ett avsnitt i klassens vy. */

type Draft = { title: string; body: string; preview: boolean; busy: boolean; problem?: Problem };

const emptyDraft = (): Draft => ({ title: '', body: '', preview: false, busy: false });

export function createBoard(options: {
  classId: string;
  me: Me;
  myRole: Role | null;
  alive: () => boolean;
  update: () => void;
}) {
  const { classId, me, myRole, alive, update } = options;
  const canPublish = myRole === 'Teacher' || myRole === 'StudentRepresentative';
  const canPin = myRole === 'Teacher';
  const state = {
    posts: [] as Post[],
    loadError: undefined as string | undefined,
    draft: emptyDraft(),
    editing: new Map<string, Draft>(),
    actionError: undefined as string | undefined,
  };

  const load = async () => {
    const result = await api.posts(classId);
    if (!alive()) return;
    if (result.ok) {
      state.posts = result.data;
      state.loadError = undefined;
    } else {
      state.loadError = problemMessage(result.problem);
    }
  };

  const publish = async (event: Event) => {
    event.preventDefault();
    const d = state.draft;
    d.busy = true;
    d.problem = undefined;
    update();
    const result = await api.publish(classId, d.title.trim(), d.body.trim());
    if (!alive()) return;
    d.busy = false;
    if (result.ok) {
      state.draft = emptyDraft();
      await load();
      await toast('Inlägget är publicerat.');
    } else {
      d.problem = result.problem;
    }
    update();
  };

  const saveEdit = async (event: Event, post: Post) => {
    event.preventDefault();
    const d = state.editing.get(post.id)!;
    d.busy = true;
    d.problem = undefined;
    update();
    const result = await api.editPost(classId, post.id, d.title.trim(), d.body.trim());
    if (!alive()) return;
    d.busy = false;
    if (result.ok) {
      state.editing.delete(post.id);
      await load();
      await toast('Ändringen är sparad.');
    } else {
      d.problem = result.problem;
    }
    update();
  };

  const remove = async (post: Post) => {
    const alert = await alertController.create({
      header: 'Ta bort inlägget?',
      message: `"${post.title}" försvinner från anslagstavlan för alla i klassen.`,
      buttons: [
        { text: 'Avbryt', role: 'cancel' },
        { text: 'Ta bort', role: 'destructive' },
      ],
    });
    await alert.present();
    const { role } = await alert.onDidDismiss();
    if (role !== 'destructive' || !alive()) return;
    const result = await api.deletePost(classId, post.id);
    if (!alive()) return;
    state.actionError = result.ok ? undefined : problemMessage(result.problem);
    await load();
    if (result.ok) await toast('Inlägget är borttaget.');
    update();
  };

  const pin = async (post: Post, pinned: boolean) => {
    const result = await api.pinPost(classId, post.id, pinned);
    if (!alive()) return;
    state.actionError = result.ok ? undefined : problemMessage(result.problem);
    await load();
    update();
  };

  const form = (d: Draft, submit: (e: Event) => void, submitLabel: string, busyLabel: string, cancel?: () => void) => html`
    <form class="stack" @submit=${submit} novalidate>
      <ion-input
        label="Rubrik"
        label-placement="stacked"
        fill="outline"
        required
        maxlength="150"
        error-text=${fieldError(d.problem, 'title') ?? ''}
        class=${invalidClass(fieldError(d.problem, 'title'))}
        .value=${d.title}
        @ionInput=${(e: Event) => (d.title = inputValue(e))}
      ></ion-input>
      <ion-textarea
        label="Text"
        label-placement="stacked"
        fill="outline"
        required
        auto-grow
        rows="5"
        maxlength="10000"
        helper-text="Du kan använda Markdown: **fetstil**, *kursiv*, listor med - och länkar som [text](https://…)."
        error-text=${fieldError(d.problem, 'body') ?? ''}
        class=${invalidClass(fieldError(d.problem, 'body'))}
        .value=${d.body}
        @ionInput=${(e: Event) => (d.body = inputValue(e))}
      ></ion-textarea>
      <ion-button fill="outline" @click=${() => { d.preview = !d.preview; update(); }}>
        ${d.preview ? 'Dölj förhandsgranskning' : 'Förhandsgranska'}
      </ion-button>
      ${d.preview
        ? html`<div class="post post--preview" aria-label="Förhandsgranskning">
            <h3>${d.title || 'Rubrik'}</h3>
            <div class="post__body">${renderMarkdown(d.body)}</div>
          </div>`
        : nothing}
      ${errorText(d.problem && d.problem.status !== 400 ? problemMessage(d.problem) : undefined)}
      ${submitButton(submitLabel, busyLabel, d.busy)}
      ${cancel ? html`<ion-button fill="clear" @click=${cancel}>Avbryt</ion-button>` : nothing}
    </form>
  `;

  const postView = (post: Post): TemplateResult => {
    const headingId = `post-${post.id}`;
    const editing = state.editing.get(post.id);
    const isAuthor = post.author.id === me.id && !post.deleted;
    const meta = [
      post.author.displayName,
      post.author.role ? roleLabel(post.author.role) : null,
      formatDateTime(post.createdAt),
      post.editedAt ? 'redigerad' : null,
    ].filter(Boolean).join(' · ');

    if (editing) {
      return html`<article class="post" aria-labelledby=${headingId}>
        <h3 id=${headingId}>Redigera "${post.title}"</h3>
        ${form(editing, (e) => saveEdit(e, post), 'Spara ändringar', 'Sparar …', () => { state.editing.delete(post.id); update(); })}
      </article>`;
    }

    return html`
      <article class="post ${post.deleted ? 'post--deleted' : ''}" aria-labelledby=${headingId}>
        ${post.pinnedAt ? html`<p class="post__pinned">Fäst</p>` : nothing}
        <h3 id=${headingId}>${post.title}</h3>
        <p class="post__meta">${meta}</p>
        ${post.deleted
          ? html`<p class="post__deleted" role="note">
              Borttaget av ${post.deleted.byDisplayName} ${formatDateTime(post.deleted.at)}. Raderas permanent
              ${formatDateTime(post.deleted.purgeAt)}.
            </p>`
          : nothing}
        <div class="post__body">${renderMarkdown(post.body)}</div>
        ${(isAuthor || (canPin && !post.deleted))
          ? html`<div class="post__actions">
              ${isAuthor
                ? html`
                    <ion-button size="small" fill="outline" @click=${() => {
                      state.editing.set(post.id, { title: post.title, body: post.body, preview: false, busy: false });
                      update();
                    }}>Redigera<span class="visually-hidden"> ${post.title}</span></ion-button>
                    <ion-button size="small" fill="outline" color="danger" @click=${() => remove(post)}>
                      Ta bort<span class="visually-hidden"> ${post.title}</span>
                    </ion-button>
                  `
                : nothing}
              ${canPin && !post.deleted
                ? html`<ion-button size="small" fill="clear" @click=${() => pin(post, !post.pinnedAt)}>
                    ${post.pinnedAt ? 'Lossa' : 'Fäst'}<span class="visually-hidden"> ${post.title}</span>
                  </ion-button>`
                : nothing}
            </div>`
          : nothing}
      </article>
    `;
  };

  const view = (): TemplateResult => html`
    <section class="section" aria-labelledby="board">
      <h2 id="board">Anslagstavla</h2>
      ${canPublish
        ? html`<details class="new-post">
            <summary>Nytt inlägg</summary>
            ${form(state.draft, publish, 'Publicera', 'Publicerar …')}
          </details>`
        : nothing}
      ${errorText(state.loadError ?? state.actionError)}
      ${state.posts.length ? state.posts.map(postView) : html`<p>Här finns inga inlägg än.</p>`}
    </section>
  `;

  return { load, view };
}
