import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const accessToken = auth.getAccessToken();

    const outgoingRequest = accessToken
        ? request.clone({
            setHeaders: {
                Authorization: `Bearer ${accessToken}`
            }
        }) : request;

    return next(outgoingRequest).pipe(
        catchError(error => {
            if (error.status === 401) {
                auth.logout();
                router.navigate(['/login']);
            }

            return throwError(() => error);
        })
    );
}