import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { AdminSupportMessageDetail } from './admin-support-message-detail';
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

describe('AdminSupportMessageDetail', () => {
  let fixture: ComponentFixture<AdminSupportMessageDetail>;
  let component: AdminSupportMessageDetail;
  let serviceStub: { getMessage: ReturnType<typeof vi.fn>; reply: ReturnType<typeof vi.fn> };

  function createComponent(message: AdminSupportMessage): void {
    serviceStub = {
      getMessage: vi.fn().mockReturnValue(of(message)),
      reply: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [AdminSupportMessageDetail],
      providers: [
        provideRouter([]),
        { provide: AdminSupportMessagesService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'm-1' }) } } },
      ],
    });

    fixture = TestBed.createComponent(AdminSupportMessageDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads the message and pre-fills the reply form with any existing reply', () => {
    createComponent(buildMessage({ replyMessage: 'Already answered.' }));

    expect(component['message']()?.id).toBe('m-1');
    expect(component['replyForm'].value.replyMessage).toBe('Already answered.');
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getMessage: vi.fn().mockReturnValue(throwError(() => new Error('boom'))), reply: vi.fn() };
    TestBed.configureTestingModule({
      imports: [AdminSupportMessageDetail],
      providers: [
        provideRouter([]),
        { provide: AdminSupportMessagesService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'm-1' }) } } },
      ],
    });
    fixture = TestBed.createComponent(AdminSupportMessageDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component['loadError']()).toBe(true);
  });

  it('does not submit an empty reply', () => {
    createComponent(buildMessage());
    component['replyForm'].setValue({ replyMessage: '' });

    component['submitReply']();

    expect(serviceStub.reply).not.toHaveBeenCalled();
  });

  it('submits the reply and refetches on success', () => {
    createComponent(buildMessage());
    component['replyForm'].setValue({ replyMessage: 'Here is the answer.' });
    serviceStub.reply.mockReturnValue(of(undefined));

    component['submitReply']();

    expect(serviceStub.reply).toHaveBeenCalledWith('m-1', 'Here is the answer.');
    expect(serviceStub.getMessage).toHaveBeenCalledTimes(2);
  });

  it('surfaces a server error without refetching', () => {
    createComponent(buildMessage());
    component['replyForm'].setValue({ replyMessage: 'Here is the answer.' });
    serviceStub.reply.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400 })));

    component['submitReply']();

    expect(component['submitError']()).toBeTruthy();
    expect(serviceStub.getMessage).toHaveBeenCalledTimes(1);
  });
});
