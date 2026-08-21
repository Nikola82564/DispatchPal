import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Customer } from '../../../core/models/customer';
import { DispatchPalApiService } from '../../../core/services/dispatch-pal-api';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-customer-list',
  imports: [FormsModule, RouterLink],
  templateUrl: './customer-list.html',
  styleUrl: './customer-list.scss'
})
export class CustomerList implements OnInit {
  private readonly api = inject(DispatchPalApiService);
  private readonly changeDetectorRef =
    inject(ChangeDetectorRef);

  customers: Customer[] = [];

  search = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadCustomers();
  }

  loadCustomers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.api
      .getCustomers(this.search, this.page, this.pageSize)
      .subscribe({
          next: response => {
            this.customers = response.items;
            this.page = response.page;
            this.pageSize = response.pageSize;
            this.totalCount = response.totalCount;
            this.totalPages = response.totalPages;
            this.isLoading = false;

            this.changeDetectorRef.markForCheck();
          },
        error: () => {
          this.errorMessage = 'Customers could not be loaded.';
          this.isLoading = false;

          this.changeDetectorRef.markForCheck();
        }
      });
  }

  searchCustomers(): void {
    this.page = 1;
    this.loadCustomers();
  }

  previousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.page--;
    this.loadCustomers();
  }

  nextPage(): void {
    if (this.page >= this.totalPages) {
      return;
    }

    this.page++;
    this.loadCustomers();
  }
}