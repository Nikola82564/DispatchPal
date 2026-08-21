import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';
import { DispatchPalApiService } from '../../../core/services/dispatch-pal-api';

@Component({
  selector: 'app-edit-dispatch-request',
  imports: [
    ReactiveFormsModule,
    RouterLink
  ],
  templateUrl: './edit-dispatch-request.html',
  styleUrl: './edit-dispatch-request.scss'
})
export class EditDispatchRequest implements OnInit {
  private readonly api = inject(DispatchPalApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly changeDetectorRef =
    inject(ChangeDetectorRef);

  private requestId = '';

  isLoading = true;
  isSaving = false;
  errorMessage = '';

  form = this.formBuilder.nonNullable.group({
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

  ngOnInit(): void {
    const requestId =
      this.route.snapshot.paramMap.get('id');

    if (!requestId) {
      this.errorMessage =
        'Dispatch request ID is missing.';
      this.isLoading = false;
      return;
    }

    this.requestId = requestId;

    this.api.getDispatchRequest(requestId).subscribe({
      next: request => {
        if (request.status !== 'Pending') {
          this.errorMessage =
            'Only pending dispatch requests can be edited.';

          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
          return;
        }

        this.form.setValue({
          pickupAddress: request.pickupAddress,
          deliveryAddress: request.deliveryAddress,
          packageDescription:
            request.packageDescription
        });

        this.isLoading = false;
        this.changeDetectorRef.markForCheck();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage =
          error.status === 404
            ? 'Dispatch request was not found.'
            : 'Dispatch request could not be loaded.';

        this.isLoading = false;
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const value = this.form.getRawValue();

    this.api.updateDispatchRequest(
      this.requestId,
      {
        pickupAddress: value.pickupAddress,
        deliveryAddress: value.deliveryAddress,
        packageDescription:
          value.packageDescription
      }
    ).subscribe({
      next: () => {
        this.router.navigate(['/dispatch-requests']);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage =
          error.status === 409
            ? 'This request is no longer pending and cannot be edited.'
            : 'Dispatch request could not be updated.';

        this.isSaving = false;
        this.changeDetectorRef.markForCheck();
      }
    });
  }
}