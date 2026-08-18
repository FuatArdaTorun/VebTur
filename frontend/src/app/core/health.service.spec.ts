import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { HealthService } from './health.service';

describe('HealthService', () => {
  let service: HealthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(HealthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getHealth GETs /api/v1/health', () => {
    service.getHealth().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/health'));
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'healthy', databaseReachable: true, timestampUtc: '2026-08-18T00:00:00Z' });
  });
});
