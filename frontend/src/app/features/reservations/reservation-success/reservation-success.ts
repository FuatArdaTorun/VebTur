import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-reservation-success',
  imports: [RouterLink],
  templateUrl: './reservation-success.html',
  styleUrl: './reservation-success.scss',
})
export class ReservationSuccess {
  private readonly route = inject(ActivatedRoute);

  protected readonly reference = this.route.snapshot.paramMap.get('reference') ?? '';
}
