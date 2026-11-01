import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap, map } from 'rxjs';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';

@Injectable({
  providedIn: 'root'
})
export class GroupOrdersService {
  private http = inject(HttpClient);

  getGroupInvite$(reservationId: string): Observable<any> {
    return this.http.get<{ token: string }>(`/reservations/${reservationId}/invite-token`).pipe(
      switchMap(response => this.http.get<any>(`/guest/invites/${response.token}`))
    );
  }

  getSessions$(groupReservationId: string, options?: { silent?: boolean }): Observable<any> {
    const context = options?.silent ? new HttpContext().set(SKIP_ERROR_TOAST, true) : undefined;
    return this.http.get<any>(`/guest/groups/${groupReservationId}/sessions`, { context });
  }

  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http.get<{ guestSessionId: string }>(`/reservations/${reservationId}/guest-session-id`).pipe(
      map(response => response.guestSessionId)
    );
  }

  getStatus$(groupReservationId: string, options?: { silent?: boolean }): Observable<any> {
    const context = options?.silent ? new HttpContext().set(SKIP_ERROR_TOAST, true) : undefined;
    return this.http.get<any>(`/guest/groups/${groupReservationId}/status`, { context });
  }

  submit$(groupReservationId: string, reservationId: string): Observable<any> {
    return this.http.post<any>(`/group-reservations/${groupReservationId}/submit`, { reservationId });
  }
}