import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HealthService, HealthStatus } from './core/health.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly healthService = inject(HealthService);

  protected readonly title = signal('VebTur');
  protected readonly health = signal<HealthStatus | null>(null);
  protected readonly healthError = signal<string | null>(null);

  constructor() {
    this.healthService.getHealth().subscribe({
      next: (status) => this.health.set(status),
      error: () => this.healthError.set('Could not reach the VebTur API.')
    });
  }
}
