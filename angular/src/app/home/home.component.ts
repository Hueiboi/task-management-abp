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
  private authService = inject(AuthService); // inject authService

  // Kiểm tra đăng nhập của người dùng 
  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  // Chuyển trang sang đăng nhập
  login() {
    this.authService.navigateToLogin();
  }
}
