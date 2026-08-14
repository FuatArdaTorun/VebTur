import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { NotificationBell } from './notification-bell';
import { NotificationBellItem } from './notification-bell.model';

describe('NotificationBell', () => {
  let fixture: ComponentFixture<NotificationBell>;
  let component: NotificationBell;

  function createComponent(items: NotificationBellItem[] = [], storageKey = 'test-bell'): void {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [NotificationBell],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(NotificationBell);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('storageKey', storageKey);
    fixture.detectChanges();
  }

  afterEach(() => localStorage.clear());

  it('starts closed', () => {
    createComponent();
    expect(component['isOpen']()).toBe(false);
  });

  it('unreadCount counts every item when none are read yet', () => {
    createComponent([
      { id: '1', title: 'A', routerLink: ['/a'] },
      { id: '2', title: 'B', routerLink: ['/b'] },
    ]);

    expect(component['unreadCount']()).toBe(2);
  });

  it('toggling open emits the opened event', () => {
    createComponent();
    const openedSpy = vi.fn();
    component.opened.subscribe(openedSpy);

    component['toggle']();

    expect(component['isOpen']()).toBe(true);
    expect(openedSpy).toHaveBeenCalledTimes(1);
  });

  it('toggling closed again does not re-emit opened', () => {
    createComponent();
    const openedSpy = vi.fn();
    component.opened.subscribe(openedSpy);

    component['toggle'](); // open
    component['toggle'](); // close

    expect(component['isOpen']()).toBe(false);
    expect(openedSpy).toHaveBeenCalledTimes(1);
  });

  it('close() closes the dropdown', () => {
    createComponent();
    component['toggle']();

    component['close']();

    expect(component['isOpen']()).toBe(false);
  });

  it('a click outside the component closes an open dropdown', () => {
    createComponent();
    component['toggle']();
    expect(component['isOpen']()).toBe(true);

    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(component['isOpen']()).toBe(false);
  });

  it('dismiss removes the item from visibleItems and the unread count, but not from the underlying items()', () => {
    createComponent([{ id: '1', title: 'A', routerLink: ['/a'] }]);

    component['dismiss']('1');

    expect(component['isRead']('1')).toBe(true);
    expect(component['unreadCount']()).toBe(0);
    expect(component['visibleItems']()).toHaveLength(0);
    expect(component['items']()).toHaveLength(1);
  });

  it('dismissing one item leaves the others visible and unread', () => {
    createComponent([
      { id: '1', title: 'A', routerLink: ['/a'] },
      { id: '2', title: 'B', routerLink: ['/b'] },
    ]);

    component['dismiss']('1');

    expect(component['isRead']('1')).toBe(true);
    expect(component['isRead']('2')).toBe(false);
    expect(component['visibleItems']().map((i) => i.id)).toEqual(['2']);
    expect(component['unreadCount']()).toBe(1);
  });

  it('dismissing an item removes it from the rendered dropdown entirely', () => {
    createComponent([{ id: '1', title: 'A', routerLink: ['/a'] }]);
    component['toggle']();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.notification-bell__item')).not.toBeNull();

    component['dismiss']('1');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.notification-bell__item')).toBeNull();
    expect(fixture.nativeElement.querySelector('.notification-bell__empty')).not.toBeNull();
  });

  it('read state persists across a fresh component instance with the same storageKey', () => {
    createComponent([{ id: '1', title: 'A', routerLink: ['/a'] }], 'shared-key');
    component['dismiss']('1');

    createComponent([{ id: '1', title: 'A', routerLink: ['/a'] }], 'shared-key');

    expect(component['isRead']('1')).toBe(true);
  });

  it('read state does not leak across different storageKeys', () => {
    createComponent([{ id: '1', title: 'A', routerLink: ['/a'] }], 'bell-a');
    component['dismiss']('1');

    createComponent([{ id: '1', title: 'A', routerLink: ['/a'] }], 'bell-b');

    expect(component['isRead']('1')).toBe(false);
  });
});
