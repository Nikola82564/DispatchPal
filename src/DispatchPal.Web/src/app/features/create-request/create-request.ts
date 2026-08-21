import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DispatchPalApiService } from '../../core/services/dispatch-pal-api';
import { DispatchRequest } from '../../core/models/dispatch-request';
import { HttpErrorResponse } from '@angular/common/http';
import {
  catchError,
  switchMap,
  takeWhile,
  throwError,
  timer
} from 'rxjs';

@Component({
  selector: 'app-create-request',
  imports: [ReactiveFormsModule],
  templateUrl: './create-request.html',
  styleUrl: './create-request.scss',
})
export class CreateRequest {
  private readonly changeDetector =
  inject(ChangeDetectorRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(DispatchPalApiService);

  isSubmitting = false;
  errorMessage = '';
  createdRequest: DispatchRequest | null = null;

  form = this.formBuilder.nonNullable.group({
    customerName: [
      '',
      [
        Validators.required,
        Validators.maxLength(200)
      ]
    ],

    customerEmail: [
      '',
      [
        Validators.required,
        Validators.email,
        Validators.maxLength(320)
      ]
    ],

    customerPhone: [
      '',
      [
        Validators.maxLength(30)
      ]
    ],

    pickupAddress: [
      '',
      [
        Validators.required,
        Validators.maxLength(500)
      ]
    ],

    deliveryAddress: [
      '',
      [
        Validators.required,
        Validators.maxLength(500)
      ]
    ],

    packageDescription: [
      '',
      [
        Validators.required,
        Validators.maxLength(1000)
      ]
    ]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.createdRequest = null;

    const value = this.form.getRawValue();

    this.api.createCustomer({
      name: value.customerName,
      email: value.customerEmail,
      phone: value.customerPhone
    }).pipe(
      switchMap(customer =>
        this.api.createDispatchRequest({
          customerId: customer.id,
          pickupAddress: value.pickupAddress,
          deliveryAddress: value.deliveryAddress,
          packageDescription: value.packageDescription
        })
      )
    ).subscribe({
      next: request => {
        this.createdRequest = request;
        this.isSubmitting = false;
        this.form.reset();

        this.refreshRequestStatus(request.id);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = this.getErrorMessage(error);
        this.isSubmitting = false;
      }
    });
  }
  private refreshRequestStatus(requestId: string): void {
    timer(1000, 1000).pipe(
      switchMap(() => this.api.getDispatchRequest(requestId)),
      takeWhile(request => request.status === 'Pending', true)
    ).subscribe({
      next: request => {
        this.createdRequest = request;

        this.changeDetector.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Could not refresh request status.';

        this.changeDetector.markForCheck();
      }
    });
  }

  private getErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'The server is unavailable. Try again later.';
  }

  if (error.status === 400) {
    return 'Some entered data is invalid.';
  }

  if (error.status === 404) {
    return 'The requested resource was not found.';
  }

  if (error.status === 409) {
    return 'A customer with this email already exists.';
  }

  return 'An unexpected error occurred. Try again later.';
 }
}