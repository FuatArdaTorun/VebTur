import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-profile-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './profile-shell.html',
  styleUrl: './profile-shell.scss',
})
export class ProfileShell {}
