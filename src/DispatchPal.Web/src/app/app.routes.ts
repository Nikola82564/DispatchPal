import { Routes } from '@angular/router';
import { CreateRequest } from './features/create-request/create-request';
import { RequestDetails } from './features/request-details/request-details';
import { Login } from './features/login/login';
import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
  {
  path: 'login',
  component: Login
  },
  {
    path: '',
    component: CreateRequest,
    canActivate: [authGuard]
  },
  {
    path: 'requests',
    component: RequestDetails,
    canActivate: [authGuard]
  },
  {
  path: 'customers',
  canActivate: [authGuard],
  loadComponent: () =>
    import('./features/customers/customer-list/customer-list')
      .then(component => component.CustomerList)
  },
  {
  path: 'customers/:id',
  canActivate: [authGuard],
  loadComponent: () =>
    import('./features/customers/customer-details/customer-details')
      .then(component => component.CustomerDetails)
  },
  {
  path: 'dispatch-requests',
  canActivate: [authGuard],
  loadComponent: () =>
    import(
      './features/dispatch-requests/dispatch-request-list/dispatch-request-list'
    ).then(component => component.DispatchRequestList)
  },
  {
  path: 'dispatch-requests/:id/edit',
  canActivate: [authGuard],
  loadComponent: () =>
    import(
      './features/dispatch-requests/edit-dispatch-request/edit-dispatch-request'
    ).then(component => component.EditDispatchRequest)
  },
  {
  path: '**',
  redirectTo: ''
  },
];