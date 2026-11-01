import { ChangeDetectionStrategy, Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { finalize, interval, Subject, takeUntil, exhaustMap, filter, Observable, forkJoin } from 'rxjs';


@Component({
  selector: 'app-group-order-page',
  standalone: true,
  imports: [ParticipantCardComponent, DatePipe, CurrencyPipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupOrderPageComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private groupOrdersService = inject(GroupOrdersService);
  private destroy$ = new Subject<void>();

  readonly isLoading = signal<boolean>(true);
  readonly hasError = signal<boolean>(false);
  readonly isSubmitting = signal<boolean>(false);

  readonly invite = signal<any>(null);
  readonly participants = signal<any[]>([]);
  readonly guestSessionId = signal<string | null>(null);
  readonly orderStatus = signal<string>('Open');

  readonly lastUpdatedTime = signal<string | null>(null);
  readonly refreshError = signal<boolean>(false);

  readonly groupReservationId = computed(() => this.invite()?.GroupReservationId);
readonly consolidatedOrder = computed(() => {
  const res = consolidatePreOrder(this.participants());
  return Array.isArray(res) ? res : [];
});
  readonly grandTotal = computed(() => {
    const items = this.consolidatedOrder();
    if (!Array.isArray(items)) return 0;
    return items.reduce((acc: number, item: any) => acc + (item.price * item.quantity), 0);
  });

  readonly unsubmittedCount = computed(() => 
    this.participants().filter(p => p.status !== 'Submitted').length
  );

  readonly canSubmit = computed(() => {
    return this.orderStatus() === 'Open' && 
           this.consolidatedOrder().length > 0 && 
           !this.isSubmitting();
  });

  readonly isOrderEmpty = computed(() => this.participants().every(p => p.items.length === 0));

  ngOnInit(): void {
    this.initialLoad();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initialLoad(): void {
    const reservationId = this.route.snapshot.paramMap.get('reservationId');
    if (!reservationId) return;

    this.isLoading.set(true);
    this.hasError.set(false);

    this.groupOrdersService.getGroupInvite$(reservationId).subscribe({
      next: (inviteData) => {
        this.invite.set(inviteData);
        const groupId = inviteData.GroupReservationId;

        this.groupOrdersService.getOwnerSessionId$(reservationId).subscribe(id => this.guestSessionId.set(id));
        
        this.fetchData(groupId, false);
        this.isLoading.set(false);

        interval(15_000)
          .pipe(
            takeUntil(this.destroy$),
            filter(() => this.orderStatus() === 'Open'),
            exhaustMap(() => this.fetchDataStream(groupId))
          )
          .subscribe();
      },
      error: () => {
        this.hasError.set(true);
        this.isLoading.set(false);
      }
    });
  }

  manualRefresh(): void {
    const groupId = this.groupReservationId();
    if (groupId) {
      this.fetchData(groupId, true);
    }
  }

  private fetchData(groupId: string, isManual: boolean): void {
    this.fetchDataStream(groupId).subscribe();
  }

  private fetchDataStream(groupId: string): Observable<any> {
    return new Observable<void>(observer => {
      let completed = 0;
      const checkDone = () => {
        completed++;
        if (completed === 2) {
          observer.next();
          observer.complete();
        }
      };

      this.groupOrdersService.getStatus$(groupId, { silent: true }).subscribe({
        next: (res) => {
          this.orderStatus.set(res.status);
          checkDone();
        },
        error: () => {
          this.refreshError.set(true);
          checkDone();
        }
      });

      this.groupOrdersService.getSessions$(groupId, { silent: true }).subscribe({
        next: (res) => {
          this.participants.set(res.participants || []);
          this.lastUpdatedTime.set(new Date().toLocaleTimeString());
          this.refreshError.set(false);
          checkDone();
        },
        error: () => {
          this.refreshError.set(true);
          checkDone();
        }
      });
    });
  }

  loadData(): void {
    this.initialLoad();
  }

  submitOrder(): void {
    const groupId = this.groupReservationId();
    const reservationId = this.route.snapshot.paramMap.get('reservationId');
    
    if (!groupId || !reservationId || !this.canSubmit()) return;

    this.isSubmitting.set(true);
    this.groupOrdersService.submit$(groupId, reservationId)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.orderStatus.set('Submitted');
        },
        error: () => {} 
      });
  }
}