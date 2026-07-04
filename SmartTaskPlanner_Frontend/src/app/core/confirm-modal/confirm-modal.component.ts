import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmModalService } from './confirm-modal.service';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-modal.component.html',
  styleUrls: ['./confirm-modal.component.css']
})
export class ConfirmModalComponent {
  public modalService = inject(ConfirmModalService);


  get isVisible(): boolean {
    if (typeof document === 'undefined') return false;
    const el = document.getElementById('confirmModal');
    return el?.classList.contains('show') ?? false;
  }
}
