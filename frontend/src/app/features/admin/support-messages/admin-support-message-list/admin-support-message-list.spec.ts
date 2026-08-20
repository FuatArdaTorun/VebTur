import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AdminSupportMessageList } from './admin-support-message-list';
import { AdminSupportMessagesService } from '../admin-support-messages.service';
import { AdminSupportMessage } from '../models/admin-support-message.model';

function buildMessage(overrides: Partial<AdminSupportMessage> = {}): AdminSupportMessage {
  return {
    id: 'm-1',
    senderName: 'Jane Guest',
    senderEmail: 'jane@example.com',
    subject: 'Question about my stay',
    message: 'Can I get an early check-in?',
    replyMessage: null,
    repliedAtUtc: null,
    createdAtUtc: '2026-08-20T00:00:00Z',
    ...overrides,
  };
}

describe('AdminSupportMessageList', () => {
  let fixture: ComponentFixture<AdminSupportMessageList>;
  let component: AdminSupportMessageList;
  let serviceStub: { getMessages: ReturnType<typeof vi.fn>; deleteMessages?: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminSupportMessageList],
      providers: [provideRouter([]), { provide: AdminSupportMessagesService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(AdminSupportMessageList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays messages on init', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(of({ items: [buildMessage()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['messages']().length).toBe(1);
    expect(component['loading']()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) };
    createComponent();

    expect(component['error']()).toBe(true);
  });

  it('shows "New" for an unreplied message and "Replied" once answered', () => {
    serviceStub = {
      getMessages: vi.fn().mockReturnValue(
        of({
          items: [buildMessage({ id: 'm-1', replyMessage: null }), buildMessage({ id: 'm-2', replyMessage: 'Answered.' })],
          page: 1,
          pageSize: 20,
          totalCount: 2,
          totalPages: 1,
        }),
      ),
    };
    createComponent();

    const statusCells: string[] = Array.from(fixture.nativeElement.querySelectorAll('td:nth-last-child(2)')).map((el) => (el as HTMLElement).textContent?.trim());
    expect(statusCells).toEqual(['New', 'Replied']);
  });

  it('applying the search re-fetches with the typed term, resetting to page 1', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();
    component['page'].set(3);

    component['searchControl'].setValue('refund');
    component['applySearch']();

    expect(serviceStub.getMessages).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'refund', page: 1 }));
  });

  it('goToPage ignores out-of-range pages', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 2 })) };
    createComponent();
    const callsBefore = serviceStub.getMessages.mock.calls.length;

    component['goToPage'](0);
    component['goToPage'](99);

    expect(serviceStub.getMessages.mock.calls.length).toBe(callsBefore);
  });

  it('selection mode is off by default, with no checkboxes rendered', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(of({ items: [buildMessage()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['selectionMode']()).toBe(false);
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Delete Message');
  });

  it('toggling selection mode on shows checkboxes; toggling it off again clears any selection', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(of({ items: [buildMessage()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    component['toggleSelectionMode']();
    fixture.detectChanges();
    expect(component['selectionMode']()).toBe(true);
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]')).not.toBeNull();

    component['toggleSelect']('m-1');
    expect(component['selectedCount']()).toBe(1);

    component['toggleSelectionMode']();
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('select-all selects every currently loaded row, and toggles them all off again', () => {
    serviceStub = {
      getMessages: vi.fn().mockReturnValue(of({ items: [buildMessage({ id: 'm-1' }), buildMessage({ id: 'm-2' })], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 })),
    };
    createComponent();

    component['toggleSelectAll']();
    expect(component['isAllSelected']()).toBe(true);
    expect(component['selectedCount']()).toBe(2);

    component['toggleSelectAll']();
    expect(component['isAllSelected']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('requestBulkDelete opens the confirm dialog only when something is selected', () => {
    serviceStub = { getMessages: vi.fn().mockReturnValue(of({ items: [buildMessage()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(false);

    component['toggleSelect']('m-1');
    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(true);
  });

  it('confirming bulk delete calls the service with the selected ids, clears selection, and refetches', () => {
    serviceStub = {
      getMessages: vi.fn().mockReturnValue(of({ items: [buildMessage({ id: 'm-1' }), buildMessage({ id: 'm-2' })], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 })),
      deleteMessages: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();
    component['toggleSelect']('m-1');
    component['requestBulkDelete']();

    component['confirmBulkDelete']();

    expect(serviceStub.deleteMessages).toHaveBeenCalledWith(['m-1']);
    expect(component['confirmingBulkDelete']()).toBe(false);
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
    expect(serviceStub.getMessages).toHaveBeenCalledTimes(2);
  });
});
