/** Ionic Core: bara de komponenter som används registreras, så att resten inte följer med i bygget. */
import { initialize } from '@ionic/core/components';
import { defineCustomElement as ionAlert } from '@ionic/core/components/ion-alert.js';
import { defineCustomElement as ionApp } from '@ionic/core/components/ion-app.js';
import { defineCustomElement as ionButton } from '@ionic/core/components/ion-button.js';
import { defineCustomElement as ionButtons } from '@ionic/core/components/ion-buttons.js';
import { defineCustomElement as ionContent } from '@ionic/core/components/ion-content.js';
import { defineCustomElement as ionHeader } from '@ionic/core/components/ion-header.js';
import { defineCustomElement as ionIcon } from 'ionicons/components/ion-icon.js';
import { defineCustomElement as ionInput } from '@ionic/core/components/ion-input.js';
import { defineCustomElement as ionItem } from '@ionic/core/components/ion-item.js';
import { defineCustomElement as ionLabel } from '@ionic/core/components/ion-label.js';
import { defineCustomElement as ionList } from '@ionic/core/components/ion-list.js';
import { defineCustomElement as ionNote } from '@ionic/core/components/ion-note.js';
import { defineCustomElement as ionSegment } from '@ionic/core/components/ion-segment.js';
import { defineCustomElement as ionSegmentButton } from '@ionic/core/components/ion-segment-button.js';
import { defineCustomElement as ionSelect } from '@ionic/core/components/ion-select.js';
import { defineCustomElement as ionSelectOption } from '@ionic/core/components/ion-select-option.js';
import { defineCustomElement as ionSpinner } from '@ionic/core/components/ion-spinner.js';
import { defineCustomElement as ionText } from '@ionic/core/components/ion-text.js';
import { defineCustomElement as ionTextarea } from '@ionic/core/components/ion-textarea.js';
import { defineCustomElement as ionTitle } from '@ionic/core/components/ion-title.js';
import { defineCustomElement as ionToast } from '@ionic/core/components/ion-toast.js';
import { defineCustomElement as ionToolbar } from '@ionic/core/components/ion-toolbar.js';

import '@ionic/core/css/core.css';
import '@ionic/core/css/normalize.css';
import '@ionic/core/css/structure.css';
import '@ionic/core/css/typography.css';
import '@ionic/core/css/padding.css';
import '@ionic/core/css/palettes/dark.system.css';

export function setupIonic(): void {
  initialize();
  for (const define of [
    ionAlert, ionApp, ionButton, ionButtons, ionContent, ionHeader, ionIcon, ionInput, ionItem, ionLabel, ionList, ionNote,
    ionSegment, ionSegmentButton, ionSelect, ionSelectOption, ionSpinner, ionText, ionTextarea, ionTitle, ionToast, ionToolbar,
  ]) define();
}
