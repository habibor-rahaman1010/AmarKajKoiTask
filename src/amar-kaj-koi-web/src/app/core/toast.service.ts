import { Injectable } from '@angular/core';
import Swal, { SweetAlertIcon } from 'sweetalert2';

/**
 * Centred SweetAlert2 alerts — the plain Swal.fire({ title, text, icon }) form,
 * dimming the page until the user acknowledges with OK.
 *
 * The four methods keep the signatures they had under PrimeNG's MessageService so
 * the ~40 call sites across the app did not have to change; only what appears on
 * screen did.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  public success(detail: string, summary = 'Success'): void {
    this.show('success', detail, summary);
  }

  public error(detail: string, summary = 'Error'): void {
    this.show('error', detail, summary);
  }

  public info(detail: string, summary = 'Info'): void {
    this.show('info', detail, summary);
  }

  public warn(detail: string, summary = 'Warning'): void {
    this.show('warning', detail, summary);
  }

  private show(icon: SweetAlertIcon, detail: string, summary: string): void {
    // Deliberately not awaited: callers announce a result and carry on, they do not
    // branch on the acknowledgement the way DialogService.confirm's callers do.
    void Swal.fire({
      title: summary,
      text: detail,
      icon,
      confirmButtonText: 'OK',
      confirmButtonColor: '#22c55e',
    });
  }
}
