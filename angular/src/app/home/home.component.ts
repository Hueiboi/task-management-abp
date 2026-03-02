// angular\src\app\home\home.component.ts
import { Component, inject } from '@angular/core';
import { AuthService, CoreModule } from '@abp/ng.core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  templateUrl: './home.component.html',
  styleUrls: ['../style/global.scss'], 
  imports: [CommonModule, CoreModule]
})
export class HomeComponent {
  private authService = inject(AuthService);

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  login() {
    this.authService.navigateToLogin();
  }
}