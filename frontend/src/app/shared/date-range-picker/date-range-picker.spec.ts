import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DateRangePicker } from './date-range-picker';

function iso(daysFromToday: number): string {
  const date = new Date();
  date.setDate(date.getDate() + daysFromToday);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

// "Today" is frozen for every test. The grid shows one month at a time, so with the real date an
// offset like "today + 10" moves into the next month during the last days of any month, and the
// cell a test wants to click is no longer on screen. Mid-month keeps the offsets below in one month.
const FROZEN_TODAY = new Date(2026, 5, 10, 12, 0, 0);

describe('DateRangePicker', () => {
  let component: DateRangePicker;
  let fixture: ComponentFixture<DateRangePicker>;

  beforeEach(async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(FROZEN_TODAY);
    await TestBed.configureTestingModule({ imports: [DateRangePicker] }).compileComponents();
    fixture = TestBed.createComponent(DateRangePicker);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function cellFor(daysFromToday: number): HTMLButtonElement {
    return fixture.nativeElement.querySelector(`[aria-label="${iso(daysFromToday)}"]`);
  }

  it('renders a full 6-week grid', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.date-range-picker__day').length).toBe(42);
  });

  it('marks a date in bookedDates as booked and disabled', () => {
    fixture.componentRef.setInput('bookedDates', [iso(5)]);
    fixture.detectChanges();

    const cell = cellFor(5);
    expect(cell.classList.contains('date-range-picker__day--booked')).toBe(true);
    expect(cell.disabled).toBe(true);
  });

  it('disables a date before today but not today itself', () => {
    fixture.detectChanges();

    expect(cellFor(-1).disabled).toBe(true);
    expect(cellFor(0).disabled).toBe(false);
  });

  it('emits checkInChange when nothing is selected yet', () => {
    fixture.detectChanges();
    const emitted = vi.fn();
    component.checkInChange.subscribe(emitted);

    cellFor(10).click();

    expect(emitted).toHaveBeenCalledWith(iso(10));
  });

  it('emits checkOutChange for a later date once check-in is set', () => {
    fixture.componentRef.setInput('checkIn', iso(10));
    fixture.detectChanges();
    const emitted = vi.fn();
    component.checkOutChange.subscribe(emitted);

    cellFor(13).click();

    expect(emitted).toHaveBeenCalledWith(iso(13));
  });

  it('restarts the selection when clicking a date at or before the current check-in', () => {
    fixture.componentRef.setInput('checkIn', iso(10));
    fixture.detectChanges();
    const checkInEmitted = vi.fn();
    const checkOutEmitted = vi.fn();
    component.checkInChange.subscribe(checkInEmitted);
    component.checkOutChange.subscribe(checkOutEmitted);

    cellFor(3).click();

    expect(checkInEmitted).toHaveBeenCalledWith(iso(3));
    expect(checkOutEmitted).toHaveBeenCalledWith('');
  });

  it('lets the user go back a month to pick an earlier date when the check-in is in the next month', () => {
    // On 24 August the check-in (today + 10) is 3 September, so the grid opens on September and
    // 27 August (today + 3) only appears after going back a month.
    vi.setSystemTime(new Date(2026, 7, 24, 12, 0, 0));
    fixture.componentRef.setInput('checkIn', iso(10));
    fixture.detectChanges();
    const checkInEmitted = vi.fn();
    component.checkInChange.subscribe(checkInEmitted);

    expect(cellFor(3)).toBeNull();
    fixture.nativeElement.querySelector('[aria-label="Previous month"]').click();
    fixture.detectChanges();
    cellFor(3).click();

    expect(checkInEmitted).toHaveBeenCalledWith(iso(3));
  });

  it('restarts the selection instead of picking a check-out that would cross a booked date', () => {
    fixture.componentRef.setInput('checkIn', iso(10));
    fixture.componentRef.setInput('bookedDates', [iso(12)]);
    fixture.detectChanges();
    const checkInEmitted = vi.fn();
    const checkOutEmitted = vi.fn();
    component.checkInChange.subscribe(checkInEmitted);
    component.checkOutChange.subscribe(checkOutEmitted);

    cellFor(15).click();

    expect(checkInEmitted).toHaveBeenCalledWith(iso(15));
    expect(checkOutEmitted).toHaveBeenCalledWith('');
  });

  it('does not let a disabled (booked) cell be clicked', () => {
    fixture.componentRef.setInput('bookedDates', [iso(5)]);
    fixture.detectChanges();
    const checkInEmitted = vi.fn();
    component.checkInChange.subscribe(checkInEmitted);

    cellFor(5).click();

    expect(checkInEmitted).not.toHaveBeenCalled();
  });

  it('navigates to the next and previous month', () => {
    fixture.detectChanges();
    const initialLabel = fixture.nativeElement.querySelector('.date-range-picker__month').textContent;

    fixture.nativeElement.querySelector('[aria-label="Next month"]').click();
    fixture.detectChanges();
    const nextLabel = fixture.nativeElement.querySelector('.date-range-picker__month').textContent;
    expect(nextLabel).not.toBe(initialLabel);

    fixture.nativeElement.querySelector('[aria-label="Previous month"]').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.date-range-picker__month').textContent).toBe(initialLabel);
  });

  it('jumps to the check-in date month on first render when one is already provided', () => {
    fixture.componentRef.setInput('checkIn', iso(60));
    fixture.detectChanges();

    const expectedLabel = new Date(
      Number(iso(60).slice(0, 4)),
      Number(iso(60).slice(5, 7)) - 1,
      1,
    ).toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

    expect(fixture.nativeElement.querySelector('.date-range-picker__month').textContent).toBe(expectedLabel);
  });
});
