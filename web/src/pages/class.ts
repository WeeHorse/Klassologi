import { html, nothing, render, type TemplateResult } from 'lit-html';
import { api, problemMessage, type Link, type Member, type Me, type Role } from '../api';
import { navigate } from '../router';
import { errorText, page, roleLabel, roles, shareLink, toast } from '../ui';
import { createBoard } from './board';

/** Klassens vy: min roll, och för lärare och administratörer inbjudan (UC-02), medlemmar, roller (UC-03) och återställning. */
export async function mountClass(root: HTMLElement, alive: () => boolean, classId: string): Promise<void> {
  const [me, cls] = await Promise.all([api.me(), api.getClass(classId)]);
  if (!alive()) return;
  if (!me.ok) return navigate('/logga-in', { replace: true });

  const back = html`<ion-button href="/">Mina klasser</ion-button>`;
  if (!cls.ok) {
    const message = cls.status === 404 ? 'Klassen finns inte.' : cls.status === 403 ? 'Du är inte med i den här klassen.' : problemMessage(cls.problem);
    render(page('Klass', html`${errorText(message)}<p><a href="/">Till Mina klasser</a></p>`, back), root);
    return;
  }

  const canManage = cls.data.myRole === 'Teacher' || me.data.isAdministrator;
  const state = {
    inviteRole: 'Student' as Role,
    inviteBusy: false,
    invitation: undefined as Link | undefined,
    inviteError: undefined as string | undefined,
    members: [] as Member[],
    membersError: undefined as string | undefined,
    memberMessage: undefined as string | undefined,
    resets: new Map<string, Link>(),
  };

  const loadMembers = async () => {
    const result = await api.members(classId);
    if (!alive()) return;
    if (result.ok) state.members = result.data;
    else state.membersError = problemMessage(result.problem);
  };
  const board = createBoard({ classId, me: me.data, myRole: cls.data.myRole, alive, update: () => update() });
  await Promise.all([board.load(), canManage ? loadMembers() : Promise.resolve()]);
  if (!alive()) return;

  const invite = async () => {
    state.inviteBusy = true;
    state.inviteError = undefined;
    update();
    const result = await api.createInvitation(classId, state.inviteRole);
    if (!alive()) return;
    state.inviteBusy = false;
    if (result.ok) state.invitation = result.data;
    else state.inviteError = problemMessage(result.problem);
    update();
  };

  const changeRole = async (member: Member, role: Role) => {
    if (role === member.role) return;
    state.memberMessage = undefined;
    const result = await api.setRole(classId, member.userId, role);
    if (!alive()) return;
    if (result.ok) await toast(`${member.displayName} är nu ${roleLabel(role).toLowerCase()}.`);
    else state.memberMessage = problemMessage(result.problem);
    await loadMembers(); // Visa alltid det sparade läget, även när ändringen nekades.
    update();
  };

  const resetPassword = async (member: Member) => {
    state.memberMessage = undefined;
    const result = await api.createPasswordReset(classId, member.userId);
    if (!alive()) return;
    if (result.ok) state.resets.set(member.userId, result.data);
    else state.memberMessage = problemMessage(result.problem);
    update();
  };

  const update = () =>
    render(
      page(
        cls.data.name,
        html`
          <p class="lead">Din roll: <strong>${cls.data.myRole ? roleLabel(cls.data.myRole) : 'Administratör'}</strong></p>
          ${board.view()}
          ${canManage ? html`${inviteSection()}${membersSection(me.data)}` : nothing}
        `,
        back,
      ),
      root,
    );

  const inviteSection = () => html`
    <section class="section" aria-labelledby="invite">
      <h2 id="invite">Bjud in</h2>
      <p>Välj roll och dela länken med den du vill bjuda in.</p>
      <ion-segment
        aria-label="Roll för inbjudan"
        .value=${state.inviteRole}
        @ionChange=${(e: CustomEvent<{ value: Role }>) => (state.inviteRole = e.detail.value)}
      >
        ${roles.map((r) => html`<ion-segment-button value=${r}><ion-label>${roleLabel(r)}</ion-label></ion-segment-button>`)}
      </ion-segment>
      <ion-button expand="block" ?disabled=${state.inviteBusy} @click=${invite}>Skapa inbjudningslänk</ion-button>
      ${errorText(state.inviteError)}
      ${state.invitation ? shareLink(`Inbjudan som ${roleLabel(state.inviteRole).toLowerCase()}`, state.invitation.url, state.invitation.expiresAt) : nothing}
    </section>
  `;

  const membersSection = (me: Me) => html`
    <section class="section" aria-labelledby="members">
      <h2 id="members">Medlemmar</h2>
      ${errorText(state.membersError ?? state.memberMessage)}
      <ion-list inset>
        ${state.members.map((m) => memberItem(m, me))}
      </ion-list>
    </section>
  `;

  const memberItem = (m: Member, me: Me): TemplateResult => {
    // Lärare kan inte återställa lösenord för lärare eller administratörer (specifikationen, steg 3; heuristik 5).
    const canReset = me.isAdministrator || (m.role !== 'Teacher' && !m.isAdministrator);
    const reset = state.resets.get(m.userId);
    return html`
      <ion-item>
        <ion-select
          label=${m.displayName}
          interface="popover"
          .value=${m.role}
          @ionChange=${(e: CustomEvent<{ value: Role }>) => changeRole(m, e.detail.value)}
        >
          ${roles.map((r) => html`<ion-select-option value=${r}>${roleLabel(r)}</ion-select-option>`)}
        </ion-select>
      </ion-item>
      ${canReset
        ? html`
            <ion-item lines="full">
              ${reset
                ? shareLink(`Nytt lösenord för ${m.displayName}`, reset.url, reset.expiresAt)
                : html`<ion-button fill="clear" size="small" @click=${() => resetPassword(m)}>Återställ lösenord för ${m.displayName}</ion-button>`}
            </ion-item>
          `
        : nothing}
    `;
  };

  update();
}
