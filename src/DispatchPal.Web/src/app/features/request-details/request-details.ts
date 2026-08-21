import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { DispatchPalApiService } from '../../core/services/dispatch-pal-api';
import { DispatchRequest } from '../../core/models/dispatch-request';

@Component({
  selector: 'app-request-details',
  imports: [ReactiveFormsModule],
  templateUrl: './request-details.html',
  styleUrl: './request-details.scss'
})
export class RequestDetails {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(DispatchPalApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  request: DispatchRequest | null = null;
  errorMessage = '';
  isLoading = false;

  form = this.formBuilder.nonNullable.group({
  requestId: ['', Validators.required]
  });

  search(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    this.request = null;

    const requestId = this.form.getRawValue().requestId.trim();

    this.api.getDispatchRequest(requestId).subscribe({
      next: request => {
        this.request = request;
        this.isLoading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.errorMessage = error.status === 404
          ? 'Dispatch request was not found.'
          : 'Could not load dispatch request.';

        this.isLoading = false;
        this.changeDetector.markForCheck();
      }
    });
  }
}
