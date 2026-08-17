import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WarningBanner } from './warning-banner';

@Component({
  imports: [WarningBanner],
  template: `
    <app-warning-banner message="Blocked.">
      <a href="/somewhere">Take action</a>
    </app-warning-banner>
  `,
})
class HostWithProjectedContent {}

describe('WarningBanner', () => {
  let component: WarningBanner;
  let fixture: ComponentFixture<WarningBanner>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WarningBanner],
    }).compileComponents();

    fixture = TestBed.createComponent(WarningBanner);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('message', 'This hotel has reservation history and cannot be permanently deleted.');
    fixture.detectChanges();
  });

  it('renders the given message inside an alert role', () => {
    expect(component).toBeTruthy();
    const alertEl = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alertEl).not.toBeNull();
    expect(alertEl.textContent).toContain('This hotel has reservation history and cannot be permanently deleted.');
  });

  it('renders projected content alongside the message', async () => {
    const hostFixture = TestBed.createComponent(HostWithProjectedContent);
    hostFixture.detectChanges();

    const link = hostFixture.nativeElement.querySelector('a');
    expect(link?.textContent).toContain('Take action');
    expect(hostFixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Blocked.');
  });
});
