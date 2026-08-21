import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink} from '@angular/router';
import { Customer } from '../../../core/models/customer';
import { DispatchPalApiService } from '../../../core/services/dispatch-pal-api';
import {
  EMPTY,
  catchError,
  switchMap
} from 'rxjs';

@Component({
  selector: 'app-customer-details',
  imports: [RouterLink],
  templateUrl: './customer-details.html',
  styleUrl: './customer-details.scss'
})
export class CustomerDetails implements OnInit {
  private readonly api = inject(DispatchPalApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly changeDetectorRef =
    inject(ChangeDetectorRef);

  customer: Customer | null = null;
  isLoading = true;
  errorMessage = '';

ngOnInit(): void {
  this.route.paramMap.pipe(
    switchMap(params => {
      const customerId = params.get('id');

      this.customer = null;
      this.errorMessage = '';
      this.isLoading = true;

      if (!customerId) {
        this.errorMessage = 'Customer ID is missing.';
        this.isLoading = false;
        this.changeDetectorRef.markForCheck();

        return EMPTY;
      }

      return this.api.getCustomer(customerId).pipe(
        catchError((error: HttpErrorResponse) => {
          this.errorMessage =
            error.status === 404
              ? 'Customer was not found.'
              : 'Customer could not be loaded.';

          this.isLoading = false;
          this.changeDetectorRef.markForCheck();

          return EMPTY;
        })
      );
    })
  ).subscribe(customer => {
    this.customer = customer;
    this.isLoading = false;
    this.changeDetectorRef.markForCheck();
  });
 }
}