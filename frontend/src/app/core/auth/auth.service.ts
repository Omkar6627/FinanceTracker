import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AcceptInvitationRequest,
  AuthResponse,
  LoginRequest,
  MemberRole,
  RegisterRequest,
  UserProfile,
} from '../models/api.models';
import { canRole, Permission } from './permissions';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly currentUser = signal<UserProfile | null>(null);

  constructor(private http: HttpClient, private tokens: TokenService) {
    const stored = this.tokens.getUser<UserProfile>();
    if (stored) this.currentUser.set(stored);
  }

  get isAuthenticated(): boolean {
    return !!this.tokens.getAccessToken() && !this.tokens.isExpired();
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, req)
      .pipe(tap((r) => this.persist(r)));
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, req)
      .pipe(tap((r) => this.persist(r)));
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.tokens.getRefreshToken() ?? '';
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, { refreshToken })
      .pipe(tap((r) => this.persist(r)));
  }

  logout(): void {
    this.tokens.clear();
    this.currentUser.set(null);
  }

  acceptInvitation(req: AcceptInvitationRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/invite/accept`, req)
      .pipe(tap((r) => this.persist(r)));
  }

  get role(): MemberRole | null {
    return (this.currentUser()?.role as MemberRole) ?? null;
  }

  get isEnterprise(): boolean {
    return this.currentUser()?.organisationMode === 'Enterprise';
  }

  can(permission: Permission | string): boolean {
    return canRole(this.role, permission);
  }

  updateUser(patch: Partial<UserProfile>): void {
    const current = this.currentUser();
    if (!current) return;
    const merged = { ...current, ...patch };
    this.currentUser.set(merged);
    this.tokens.setUser(merged);
  }

  private persist(r: AuthResponse): void {
    this.tokens.setTokens(r.accessToken, r.refreshToken, r.expiresAt);
    this.tokens.setUser(r.user);
    this.currentUser.set(r.user);
  }
}
