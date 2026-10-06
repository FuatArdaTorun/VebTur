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

  it('renders the website, app, and WhatsApp contact items as placeholder links that lead nowhere', () => {
    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('.footer__contact-link'));

    expect(links.map((a) => a.textContent?.trim())).toEqual(['Our Website', 'Get the App', 'WhatsApp']);
    expect(links.every((a) => a.getAttribute('href') === '#')).toBe(true);
    expect(links.every((a) => !a.hasAttribute('target'))).toBe(true);
  });

  it('cancels the click on every contact link, so the page neither navigates nor jumps to the top', () => {
    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('.footer__contact-link'));

    for (const link of links) {
      const click = new MouseEvent('click', { bubbles: true, cancelable: true });
      link.dispatchEvent(click);
      expect(click.defaultPrevented).toBe(true);
    }
  });

  it('marks every contact link as a demo link, with a hover tooltip and a screen-reader description', () => {
    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('.footer__contact-link'));
    const note: HTMLElement = fixture.nativeElement.querySelector('#footer-demo-note');

    expect(links.every((a) => a.dataset['tooltip'] === 'Demo link')).toBe(true);
    expect(links.every((a) => a.getAttribute('aria-describedby') === 'footer-demo-note')).toBe(true);
    expect(note.textContent?.trim()).toBe('Demo link');
    expect(note.hidden).toBe(true);
  });

  it('shows no phone number and no real external, WhatsApp, or tel: link anywhere in the footer', () => {
    const footer: HTMLElement = fixture.nativeElement;

    expect(footer.textContent).not.toMatch(/(\d[\s-]?){7,}/);
    expect(footer.querySelector('a[href^="http"], a[href^="tel:"]')).toBeNull();
  });
});
