import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { NotFound } from './not-found';

describe('NotFound', () => {
  let fixture: ComponentFixture<NotFound>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [NotFound], providers: [provideRouter([])] });
    fixture = TestBed.createComponent(NotFound);
    fixture.detectChanges();
  });

  it('renders a 404 message', () => {
    expect(fixture.nativeElement.textContent).toContain('404');
    expect(fixture.nativeElement.textContent).toContain('Page Not Found');
  });

  it('links back to home and to the hotel list', () => {
    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('a'));
    expect(links.some((a) => a.getAttribute('href') === '/')).toBe(true);
    expect(links.some((a) => a.getAttribute('href') === '/hotels')).toBe(true);
  });
});
