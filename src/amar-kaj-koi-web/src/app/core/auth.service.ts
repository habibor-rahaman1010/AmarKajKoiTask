import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UserProfile } from './models';

const TOKEN_KEY = 'akk_token';
const USER_KEY = 'akk_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _user = signal<UserProfile | null>(this.loadUser());

  public readonly user = this._user.asReadonly();
  public readonly role = computed(() => this._user()?.roleName ?? '');
  public readonly isAuthenticated = computed(() => !!this._user() && !!this.token());

  public token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  public login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { username, password })
      .pipe(
        tap((resp) => {
          localStorage.setItem(TOKEN_KEY, resp.token);
          localStorage.setItem(USER_KEY, JSON.stringify(resp.user));
          this._user.set(resp.user);
        }),
      );
  }

  public logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._user.set(null);
    this.router.navigateByUrl('/login');
  }

  public hasAnyRole(roles: string[]): boolean {
    const r = this._user()?.roleName ?? '';
    return roles.includes(r);
  }

  private loadUser(): UserProfile | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as UserProfile;
    } catch {
      return null;
    }
  }
}