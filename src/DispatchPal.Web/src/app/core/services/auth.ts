import { Injectable } from '@angular/core';
import { LoginResponse } from '../models/login-response';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenKey = 'dispatchpal_access_token';
  private readonly expirationKey = 'dispatchpal_token_expiration';

  saveSession(response: LoginResponse): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    localStorage.setItem(this.expirationKey, response.expiresAtUtc);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  logout(): void{
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.expirationKey);
  }

  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    const expiration = localStorage.getItem(this.expirationKey);

    if(!token || !expiration) {
        return false;
    }

    return new Date(expiration).getTime() > Date.now();
  }
}