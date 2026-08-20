import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { HelpService } from './help.service';

describe('HelpService', () => {
  let service: HelpService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(HelpService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sendMessage POSTs /support-messages with the given body', () => {
    const dto = { name: 'Jane Guest', email: 'jane@example.com', subject: 'Question', message: 'Hello there.' };
    service.sendMessage(dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/support-messages'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({ id: 'msg-1', createdAtUtc: '2026-08-20T00:00:00Z' });
  });
});
