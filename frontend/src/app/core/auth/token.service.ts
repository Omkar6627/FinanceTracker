import { Injectable } from '@angular/core';

const ACCESS_KEY = 'ft.accessToken';
const REFRESH_KEY = 'ft.refreshToken';
const EXPIRES_KEY = 'ft.expiresAt';
const USER_KEY = 'ft.user';

@Injectable({ providedIn: 'root' })
export class TokenService {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  getExpiresAt(): Date | null {
    const raw = localStorage.getItem(EXPIRES_KEY);
    return raw ? new Date(raw) : null;
  }

  setTokens(access: string, refresh: string, expiresAt: string): void {
    localStorage.setItem(ACCESS_KEY, access);
    localStorage.setItem(REFRESH_KEY, refresh);
    localStorage.setItem(EXPIRES_KEY, expiresAt);
  }

  setUser(user: unknown): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }

  getUser<T = unknown>(): T | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as T) : null;
  }

  clear(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    localStorage.removeItem(USER_KEY);
  }

  isExpired(): boolean {
    const expiresAt = this.getExpiresAt();
    if (!expiresAt) return true;
    return expiresAt.getTime() - Date.now() < 10_000;
  }
}
