import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DispatchRequestListItem } from '../../../core/models/dispatch-request-list-item';
import { DispatchPalApiService } from '../../../core/services/dispatch-pal-api';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dispatch-request-list',
  imports: [FormsModule, RouterLink],
  templateUrl: './dispatch-request-list.html',
  styleUrl: './dispatch-request-list.scss',
})
export class DispatchRequestList implements OnInit {
  private readonly api = inject(DispatchPalApiService);
  private readonly changeDetectorRef =
    inject(ChangeDetectorRef);

  requests: DispatchRequestListItem[] = [];

  search = '';
  status = '';

  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.api.getDispatchRequests({
      search: this.search,
      status: this.status,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: response => {
        this.requests = response.items;
        this.page = response.page;
        this.pageSize = response.pageSize;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.isLoading = false;

        this.changeDetectorRef.markForCheck();
      },
      error: () => {
        this.errorMessage =
          'Dispatch requests could not be loaded.';

        this.isLoading = false;
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.loadRequests();
  }

  clearFilters(): void {
    this.search = '';
    this.status = '';
    this.page = 1;

    this.loadRequests();
  }

  previousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.page--;
    this.loadRequests();
  }

  nextPage(): void {
    if (this.page >= this.totalPages) {
      return;
    }

    this.page++;
    this.loadRequests();
  }
}
