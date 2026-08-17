import { Component, input } from '@angular/core';

@Component({
  selector: 'app-warning-banner',
  imports: [],
  templateUrl: './warning-banner.html',
  styleUrl: './warning-banner.scss',
})
export class WarningBanner {
  readonly message = input.required<string>();
}
