import { html, nothing, type TemplateResult } from 'lit-html';
import { classMap } from 'lit-html/directives/class-map.js';
import { toastController } from '@ionic/core/components';
import { eye, eyeOff } from 'ionicons/icons';

/** Gemensam ram för alla vyer: rubrik, eventuella knappar och innehåll. Sätter också sidans titel (WCAG 2.4.2). */
export function page(title: string, content: TemplateResult, actions?: TemplateResult): TemplateResult {
  document.title = `${title} – Klassologi`;
  return html`
    <ion-header>
      <ion-toolbar>
        <ion-title><h1 class="page-title">${title}</h1></ion-title>
        ${actions ? html`<ion-buttons slot="end">${actions}</ion-buttons>` : nothing}
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <div class="container">${content}</div>
    </ion-content>
  `;
}

export { absoluteLink, formatDateTime, roleLabel, roles } from './labels';
import { absoluteLink, formatDateTime } from './labels';

export async function toast(message: string): Promise<void> {
  const t = await toastController.create({ message, duration: 3000, position: 'bottom' });
  await t.present();
}

export async function copy(text: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(text);
    await toast('Länken är kopierad.');
  } catch {
    await toast('Kunde inte kopiera. Markera länken och kopiera den själv.');
  }
}

/** En engångslänk att dela, med kopieringsknapp och giltighetstid (heuristik 1 och 6). */
export function shareLink(label: string, url: string, expiresAt: string): TemplateResult {
  const link = absoluteLink(url);
  return html`
    <div class="share-link" role="group" aria-label=${label}>
      <p class="share-link__label">${label}</p>
      <p class="share-link__url" data-testid="share-link">${link}</p>
      <p class="share-link__expires">Gäller till ${formatDateTime(expiresAt)}. Länken kan bara användas en gång.</p>
      <ion-button fill="outline" @click=${() => copy(link)}>Kopiera länk</ion-button>
    </div>
  `;
}

export function errorText(message: string | undefined): TemplateResult | typeof nothing {
  return message ? html`<ion-text color="danger"><p class="form-error" role="alert">${message}</p></ion-text>` : nothing;
}

export function invalidClass(error: string | undefined) {
  return classMap({ 'ion-invalid': !!error, 'ion-touched': !!error });
}

type InputEvent = CustomEvent<{ value?: string | null }>;
export const inputValue = (e: Event): string => (e as InputEvent).detail.value ?? '';

/** Lösenordsfält med egen visa/dölj-knapp på svenska (Ionics inbyggda har engelsk etikett). */
export function passwordInput(options: {
  label: string;
  value: string;
  visible: boolean;
  error?: string;
  autocomplete: 'current-password' | 'new-password';
  helper?: string;
  onInput: (value: string) => void;
  onToggle: () => void;
}): TemplateResult {
  return html`
    <ion-input
      label=${options.label}
      label-placement="stacked"
      fill="outline"
      type=${options.visible ? 'text' : 'password'}
      autocomplete=${options.autocomplete}
      required
      .value=${options.value}
      helper-text=${options.helper ?? ''}
      error-text=${options.error ?? ''}
      class=${invalidClass(options.error)}
      @ionInput=${(e: Event) => options.onInput(inputValue(e))}
    >
      <ion-button
        slot="end"
        fill="clear"
        aria-label=${options.visible ? 'Dölj lösenord' : 'Visa lösenord'}
        aria-pressed=${options.visible ? 'true' : 'false'}
        @click=${options.onToggle}
      >
        <ion-icon slot="icon-only" aria-hidden="true" .icon=${options.visible ? eyeOff : eye}></ion-icon>
      </ion-button>
    </ion-input>
  `;
}

export function submitButton(label: string, busyLabel: string, busy: boolean): TemplateResult {
  return html`
    <ion-button type="submit" expand="block" ?disabled=${busy}>
      ${busy ? html`<ion-spinner name="crescent" aria-hidden="true"></ion-spinner>&nbsp;${busyLabel}` : label}
    </ion-button>
  `;
}

// Vart användaren ska efter inloggning, t.ex. tillbaka till en inbjudan (A3 i UC-04).
const returnKey = 'klassologi.returnTo';
export function setReturnTo(path: string): void {
  try {
    sessionStorage.setItem(returnKey, path);
  } catch {
    /* Sessionslagring kan saknas. Då hamnar användaren på startsidan. */
  }
}
export function takeReturnTo(): string | null {
  try {
    const value = sessionStorage.getItem(returnKey);
    sessionStorage.removeItem(returnKey);
    return value;
  } catch {
    return null;
  }
}
