import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateCustomer,
  Customer
} from '../models/customer';
import { HttpParams } from '@angular/common/http';
import { PagedResponse } from '../models/paged-response';
import {
  CreateDispatchRequest,
  DispatchRequest
} from '../models/dispatch-request';
import { DispatchRequestListItem } from '../models/dispatch-request-list-item';
import { UpdateDispatchRequest } from '../models/update-dispatch-request';
import { LoginResponse } from '../models/login-response';

@Injectable({
  providedIn: 'root'
})
export class DispatchPalApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api';

  createCustomer(request: CreateCustomer): Observable<Customer> {
    return this.http.post<Customer>(
      `${this.apiUrl}/customers`,
      request
    );
  }

  getCustomer(id: string): Observable<Customer> {
    return this.http.get<Customer>(
      `${this.apiUrl}/customers/${id}`
    );
  }

  createDispatchRequest(
    request: CreateDispatchRequest
  ): Observable<DispatchRequest> {
    return this.http.post<DispatchRequest>(
      `${this.apiUrl}/dispatch-requests`,
      request
    );
  }

  getDispatchRequest(id: string): Observable<DispatchRequest> {
    return this.http.get<DispatchRequest>(
      `${this.apiUrl}/dispatch-requests/${id}`
    );
  }

  getCustomerByEmail(email: string): Observable<Customer> {
  return this.http.get<Customer>(
    '/api/customers/by-email',
    {
      params: { email }
    }
  );
 }

  getCustomers(
    search: string,
    page: number,
    pageSize: number
  ): Observable<PagedResponse<Customer>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    const trimmedSearch = search.trim();

    if (trimmedSearch) {
      params = params.set('search', trimmedSearch);
    }

    return this.http.get<PagedResponse<Customer>>(
      '/api/customers',
      { params }
    );
  }

  getDispatchRequests(filters: {
  search: string;
  status: string;
  customerId?: string;
  page: number;
  pageSize: number;
}): Observable<PagedResponse<DispatchRequestListItem>> {
  let params = new HttpParams()
    .set('page', filters.page)
    .set('pageSize', filters.pageSize);

  const search = filters.search.trim();

  if (search) {
    params = params.set('search', search);
  }

  if (filters.status) {
    params = params.set('status', filters.status);
  }

  if (filters.customerId) {
    params = params.set(
      'customerId',
      filters.customerId
    );
  }

  return this.http.get<
    PagedResponse<DispatchRequestListItem>
  >(
    '/api/dispatch-requests',
    { params }
  );
 }

 updateDispatchRequest(
  id: string,
  request: UpdateDispatchRequest
 ): Observable<DispatchRequest> {
  return this.http.put<DispatchRequest>(
    `/api/dispatch-requests/${id}`,
    request
  );
 }

 login(email: string, password: string): Observable<LoginResponse> {
  return this.http.post<LoginResponse>('/api/auth/login', {
    email,
    password
  });
 }

}