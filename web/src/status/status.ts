import { html } from 'lit-html';

type StatusResponse = { status: string; migrations: number };

export type StatusState =
  | { state: 'loading' }
  | { state: 'ok'; migrations: number }
  | { state: 'error' };

/** Hämtar API:ts status. Fel blir ett tillstånd i stället för ett undantag, så att vyn alltid kan visa något. */
export async function loadStatus(fetchFn: typeof fetch = fetch): Promise<StatusState> {
  try {
    const response = await fetchFn('/api/status');
    if (!response.ok) return { state: 'error' };
    const body = (await response.json()) as StatusResponse;
    return body.status === 'ok' ? { state: 'ok', migrations: body.migrations } : { state: 'error' };
  } catch {
    return { state: 'error' };
  }
}

export function statusMessage(status: StatusState): string {
  switch (status.state) {
    case 'loading':
      return 'Kontrollerar anslutningen …';
    case 'ok':
      return `Ansluten. Databasen har ${status.migrations} ${status.migrations === 1 ? 'migrering' : 'migreringar'}.`;
    case 'error':
      return 'Kunde inte nå servern. Kontrollera att API:t och databasen är igång och ladda sedan om sidan.';
  }
}

export const statusView = (status: StatusState) => html`<p role="status">${statusMessage(status)}</p>`;
