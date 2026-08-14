import { Component, ElementRef, HostListener, computed, effect, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationBellItem } from './notification-bell.model';

const STORAGE_PREFIX = 'vebtur_notifications_read_';

/**
 * Presentational dropdown shared by the admin "pending approvals" bell and the customer
 * "reservation status" bell — each side owns fetching its own data and just hands this component
 * a flat list of items to render.
 *
 * Read state is tracked client-side only (localStorage, keyed by `storageKey`) — it's a "have I
 * seen this in my browser" convenience, not business state, so it doesn't need a backend table.
 * Dismissing an item removes it from this dropdown, but never touches the underlying data — e.g.
 * a dismissed "pending approval" reservation is still fully visible and actionable on the admin
 * Reservations list, only the bell's reminder about it is gone.
 */
@Component({
  selector: 'app-notification-bell',
  imports: [RouterLink],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.scss',
})
export class NotificationBell {
  private readonly elementRef = inject(ElementRef);

  readonly items = input<NotificationBellItem[]>([]);
  readonly emptyMessage = input('Nothing new right now.');
  readonly ariaLabel = input('Notifications');
  /** Namespaces the read-state in localStorage — must be unique per bell instance on the page. */
  readonly storageKey = input.required<string>();

  /** Emitted every time the dropdown is opened, so the parent can refetch fresh data on demand. */
  readonly opened = output<void>();

  protected readonly isOpen = signal(false);
  private readonly readIds = signal<Set<string>>(new Set());

  protected readonly visibleItems = computed(() => this.items().filter((item) => !this.readIds().has(item.id)));
  protected readonly unreadCount = computed(() => this.visibleItems().length);

  constructor() {
    effect(() => this.readIds.set(loadReadIds(this.storageKey())));
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (this.isOpen() && !this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  protected toggle(): void {
    const next = !this.isOpen();
    this.isOpen.set(next);
    if (next) {
      this.opened.emit();
    }
  }

  protected close(): void {
    this.isOpen.set(false);
  }

  protected isRead(id: string): boolean {
    return this.readIds().has(id);
  }

  /** Removes the item from this dropdown — never touches the underlying data it represents. */
  protected dismiss(id: string): void {
    if (this.readIds().has(id)) {
      return;
    }

    const next = new Set(this.readIds());
    next.add(id);
    this.readIds.set(next);
    saveReadIds(this.storageKey(), next);
  }
}

function loadReadIds(storageKey: string): Set<string> {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + storageKey);
    return raw ? new Set(JSON.parse(raw)) : new Set();
  } catch {
    return new Set();
  }
}

function saveReadIds(storageKey: string, ids: Set<string>): void {
  localStorage.setItem(STORAGE_PREFIX + storageKey, JSON.stringify([...ids]));
}
