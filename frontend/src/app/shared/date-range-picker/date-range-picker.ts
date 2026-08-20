import { Component, computed, effect, input, output, signal } from '@angular/core';

interface DayCell {
  date: Date;
  iso: string;
  inMonth: boolean;
  disabled: boolean;
  booked: boolean;
  isToday: boolean;
  isRangeStart: boolean;
  isRangeEnd: boolean;
  inRange: boolean;
}

function formatIso(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function parseIso(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function addDays(date: Date, amount: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + amount);
}

/**
 * Month-grid check-in/check-out picker. Native `<input type="date">` can't style individual days,
 * so already-fully-booked dates (from RoomTypeAvailabilityService) need this instead.
 */
@Component({
  selector: 'app-date-range-picker',
  imports: [],
  templateUrl: './date-range-picker.html',
  styleUrl: './date-range-picker.scss',
})
export class DateRangePicker {
  readonly checkIn = input<string | null>(null);
  readonly checkOut = input<string | null>(null);
  readonly bookedDates = input<string[]>([]);

  readonly checkInChange = output<string>();
  readonly checkOutChange = output<string>();

  protected readonly weekdayLabels = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

  private readonly viewMonth = signal(startOfDay(new Date()));
  private hasSyncedInitialMonth = false;

  protected readonly monthLabel = computed(() =>
    this.viewMonth().toLocaleDateString('en-US', { month: 'long', year: 'numeric' }),
  );

  protected readonly cells = computed<DayCell[]>(() => {
    const month = this.viewMonth();
    const year = month.getFullYear();
    const monthIndex = month.getMonth();
    const firstOfMonth = new Date(year, monthIndex, 1);
    const mondayIndexedWeekday = (firstOfMonth.getDay() + 6) % 7;
    const gridStart = addDays(firstOfMonth, -mondayIndexedWeekday);

    const today = startOfDay(new Date());
    const todayIso = formatIso(today);
    const bookedSet = new Set(this.bookedDates());
    const checkInIso = this.checkIn();
    const checkOutIso = this.checkOut();

    const cells: DayCell[] = [];
    for (let i = 0; i < 42; i++) {
      const date = addDays(gridStart, i);
      const iso = formatIso(date);
      const booked = bookedSet.has(iso);
      cells.push({
        date,
        iso,
        inMonth: date.getMonth() === monthIndex,
        disabled: iso < todayIso || booked,
        booked,
        isToday: iso === todayIso,
        isRangeStart: iso === checkInIso,
        isRangeEnd: iso === checkOutIso,
        inRange: !!checkInIso && !!checkOutIso && iso > checkInIso && iso < checkOutIso,
      });
    }
    return cells;
  });

  constructor() {
    effect(() => {
      const ci = this.checkIn();
      if (ci && !this.hasSyncedInitialMonth) {
        this.hasSyncedInitialMonth = true;
        const parsed = parseIso(ci);
        this.viewMonth.set(new Date(parsed.getFullYear(), parsed.getMonth(), 1));
      }
    });
  }

  protected prevMonth(): void {
    const m = this.viewMonth();
    this.viewMonth.set(new Date(m.getFullYear(), m.getMonth() - 1, 1));
  }

  protected nextMonth(): void {
    const m = this.viewMonth();
    this.viewMonth.set(new Date(m.getFullYear(), m.getMonth() + 1, 1));
  }

  protected selectDate(cell: DayCell): void {
    if (cell.disabled) {
      return;
    }

    const currentCheckIn = this.checkIn();
    const currentCheckOut = this.checkOut();

    if (!currentCheckIn || currentCheckOut || cell.iso <= currentCheckIn || this.hasBookedDateBetween(currentCheckIn, cell.iso)) {
      this.checkInChange.emit(cell.iso);
      this.checkOutChange.emit('');
      return;
    }

    this.checkOutChange.emit(cell.iso);
  }

  private hasBookedDateBetween(checkInIso: string, checkOutIso: string): boolean {
    const booked = new Set(this.bookedDates());
    let cursor = parseIso(checkInIso);
    const end = parseIso(checkOutIso);
    while (cursor < end) {
      if (booked.has(formatIso(cursor))) {
        return true;
      }
      cursor = addDays(cursor, 1);
    }
    return false;
  }
}
