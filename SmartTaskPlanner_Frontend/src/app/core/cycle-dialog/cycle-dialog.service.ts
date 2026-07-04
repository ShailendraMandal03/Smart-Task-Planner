import { Injectable } from '@angular/core';
import { Subject, BehaviorSubject, Observable } from 'rxjs';

export type CycleDialogMode = 'confirm' | 'error';

@Injectable({
  providedIn: 'root'
})
export class CycleDialogService {
  public cyclePath: string[] = [];
  public mode: CycleDialogMode = 'error';
  
  public isVisible$ = new BehaviorSubject<boolean>(false);

  private resultSubject = new Subject<boolean>();

  open(cyclePath: string[], mode: CycleDialogMode): Observable<boolean> {
    this.cyclePath = cyclePath;
    this.mode = mode;
    this.resultSubject = new Subject<boolean>();
    this.isVisible$.next(true);
    document.body.classList.add('modal-open');
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

  private hideModal(): void {
    this.isVisible$.next(false);
    document.body.classList.remove('modal-open');
  }
}
