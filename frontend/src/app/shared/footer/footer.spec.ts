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

  it('links the website, app, and WhatsApp contact items to the right destinations', () => {
    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('.footer__contact-link'));

    expect(links.map((a) => a.href)).toEqual([
      'https://www.veboni.com/tr/',
      'https://play.google.com/store/apps/details?id=com.veboni.vebonib2b',
      'https://wa.me/905000000000',
    ]);
    expect(links.every((a) => a.target === '_blank')).toBe(true);
    expect(links.every((a) => a.rel === 'noopener noreferrer')).toBe(true);
  });

  it('shows the WhatsApp number as plain text next to its link, not as a tel: link', () => {
    const whatsappLink = fixture.nativeElement.querySelector('.footer__contact-link[href^="https://wa.me/"]');

    expect(whatsappLink.textContent).toContain('0500 000 00 00');
    expect(fixture.nativeElement.querySelector('a[href^="tel:"]')).toBeNull();
  });
});
