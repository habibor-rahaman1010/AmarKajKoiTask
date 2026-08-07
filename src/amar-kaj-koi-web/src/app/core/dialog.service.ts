import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

export interface ConfirmOptions {
  title: string;
  text?: string;
  /** Label on the accepting button. */
  confirmText?: string;
  /** Styles the accepting button red and shows a warning icon. */
  danger?: boolean;
}

export interface PromptOptions {
  title: string;
  text?: string;
  confirmText?: string;
  /** When set, an empty answer is refused with this message instead of accepted. */
  requiredMessage?: string;
  placeholder?: string;
}

/**
 * Modal confirm and prompt dialogs, backed by SweetAlert2.
 *
 * Both return promises rather than taking callbacks, so a caller reads top to
 * bottom: `if (!(await confirm(...))) return;`. That is also what replaced the
 * browser's native prompt(), which blocked the page and could not be styled.
 */
@Injectable({ providedIn: 'root' })
export class DialogService {
  public async confirm(options: ConfirmOptions): Promise<boolean> {
    const result = await Swal.fire({
      title: options.title,
      text: options.text,
      icon: options.danger ? 'warning' : 'question',
      showCancelButton: true,
      confirmButtonText: options.confirmText ?? 'Yes',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
      confirmButtonColor: options.danger ? '#dc2626' : '#22c55e',
      cancelButtonColor: '#64748b',
    });
    return result.isConfirmed;
  }

  /**
   * Asks for a line of text. Resolves to null when the user cancels, which callers
   * treat the same way they treated a null from the native prompt().
   */
  public async prompt(options: PromptOptions): Promise<string | null> {
    const result = await Swal.fire({
      title: options.title,
      text: options.text,
      input: 'textarea',
      inputPlaceholder: options.placeholder ?? '',
      inputAttributes: { 'aria-label': options.title },
      showCancelButton: true,
      confirmButtonText: options.confirmText ?? 'Submit',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
      confirmButtonColor: '#22c55e',
      cancelButtonColor: '#64748b',
      inputValidator: (value) => {
        if (options.requiredMessage && !value?.trim()) return options.requiredMessage;
        return null;
      },
    });

    // isConfirmed is false for both Cancel and a dismiss (Esc / backdrop click).
    return result.isConfirmed ? ((result.value as string) ?? '') : null;
  }
}
