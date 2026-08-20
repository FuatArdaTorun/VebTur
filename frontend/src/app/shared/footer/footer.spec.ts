import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Footer } from './footer';

describe('Footer', () => {
  let fixture: ComponentFixture<Footer>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Footer] });
    fixture = TestBed.createComponent(Footer);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the VebTur brand mark', () => {
    expect(fixture.nativeElement.querySelector('.footer__brand')?.textContent).toContain('VebTur');
  });

  it('renders the three feature highlights', () => {
    const items: string[] = Array.from(fixture.nativeElement.querySelectorAll('.footer__highlights li')).map(
      (el) => (el as HTMLElement).textContent?.trim(),
    );

    expect(items).toEqual(['Verified Hotels', 'Transparent Pricing', 'Free to Request']);
  });
});
