import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

/**
 * A lightweight service to open a single confirm modal and get back
 * a boolean result (true = confirmed, false = cancelled).
 *
 * Usage:
 *   this.confirmModal.open('Are you sure?').subscribe(confirmed => {
 *     if (confirmed) { ... }
 *   });
 */
@Injectable({
  providedIn: 'root'
})
export class ConfirmModalService {

  public message = '';

  private resultSubject = new Subject<boolean>();

  open(message: string): Observable<boolean> {
    this.message = message;
    this.resultSubject = new Subject<boolean>();
    this.showModal();
    return this.resultSubject.asObservable();
  }

  confirm(): void {
    this.resultSubject.next(true);
    this.resultSubject.complete();
    this.hideModal();
  }

  cancel(): void {
    this.resultSubject.next(false);
    this.resultSubject.complete();
    this.hideModal();
  }

  private showModal(): void {
    const el = document.getElementById('confirmModal');
    if (el) {
      el.classList.add('show');
      el.style.display = 'block';
      document.body.classList.add('modal-open');
    }
  }

  private hideModal(): void {
    const el = document.getElementById('confirmModal');
    if (el) {
      el.classList.remove('show');
      el.style.display = 'none';
      document.body.classList.remove('modal-open');
    }
  }
}
