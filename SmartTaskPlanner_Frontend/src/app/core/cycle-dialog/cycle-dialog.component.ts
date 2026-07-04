import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CycleDialogService } from './cycle-dialog.service';

@Component({
  selector: 'app-cycle-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cycle-dialog.component.html',
  styleUrls: ['./cycle-dialog.component.css']
})
export class CycleDialogComponent {
  public modalService = inject(CycleDialogService);
  public isVisible$ = this.modalService.isVisible$;
}
