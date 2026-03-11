import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, AfterViewInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements AfterViewInit {
  loginForm: FormGroup;
  forgotPasswordForm: FormGroup;
  showPassword = false;
  isForgotPasswordMode = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private toast: ToastService,
    private authService: AuthService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  toggleForgotPasswordMode() {
    this.isForgotPasswordMode = !this.isForgotPasswordMode;
    this.forgotPasswordForm.reset();
  }

  onForgotPasswordSubmit() {
    if (this.forgotPasswordForm.valid) {
      const email = this.forgotPasswordForm.get('email')?.value;
      this.toast.info('Đang xử lý', 'Vui lòng đợi...');
      this.authService.forgotPassword(email).subscribe({
        next: (res) => {
          this.toast.success('Thành công', res.message || 'Mật khẩu mới đã được gửi đến email của bạn.');
          this.toggleForgotPasswordMode(); // Quay lại form login
        },
        error: (err) => {
          this.toast.error('Có lỗi xảy ra', err.error?.message || 'Không thể khôi phục mật khẩu lúc này.');
        }
      });
    } else {
      this.forgotPasswordForm.markAllAsTouched();
    }
  }

  ngAfterViewInit(): void {
    this.initGoogleButton();
  }

  initGoogleButton(): void {
    const google = (window as any).google;
    if (!google) {
      // SDK chưa load xong, thử lại sau 500ms
      setTimeout(() => this.initGoogleButton(), 500);
      return;
    }

    google.accounts.id.initialize({
      client_id: '731740261588-jno35lom7hluee8n0oh9u5tqn7i437kb.apps.googleusercontent.com',
      callback: (response: any) => this.handleGoogleCallback(response)
    });

    const btnDiv = document.getElementById('google-login-btn');
    if (btnDiv) {
      google.accounts.id.renderButton(btnDiv, {
        theme: 'outline',
        size: 'large',
        shape: 'rectangular',
        width: btnDiv.offsetWidth || 360,
        text: 'signin_with',
        logo_alignment: 'center'
      });
    }
  }

  handleGoogleCallback(response: any): void {
    const idToken = response.credential;
    this.authService.googleLogin(idToken).subscribe({
      next: (res) => {
        const token = res.token || res.Token;
        localStorage.setItem('authToken', token);
        this.navigateByToken(token);
      },
      error: (err) => {
        const code = err.error?.errorCode;
        if (code === 'EMAIL_REGISTERED_LOCALLY') {
          this.toast.warning('Không thể đăng nhập bằng Google', err.error.message);
        } else {
          this.toast.error('Đăng nhập thất bại', err.error?.message || 'Có lỗi xảy ra. Vui lòng thử lại.');
        }
      }
    });
  }

  private navigateByToken(token: string) {
    try {
      const decoded: any = jwtDecode(token);
      const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded['role'];
      if (role === 'HR' || role === 'ADMIN' || role === 'INTERVIEWER') {
        this.router.navigate(['/hr/dashboard']);
      } else {
        this.router.navigate(['/candidate/home']);
      }
    } catch {
      this.router.navigate(['/']);
    }
  }

  onSubmit() {
    if (this.loginForm.valid) {
      const payload = this.loginForm.value;

      this.http.post<any>('/api/auth/login', payload)
        .subscribe({
          next: (res) => {
            // res.token là chuỗi JWT backend trả về
            const token = res.token;

            if (!token) {
              console.error(' Backend không trả về token!');
              this.toast.error('Đăng nhập thất bại', 'Server không trả về token.');
              return;
            }

            // 1. Lưu Token vào localStorage
            localStorage.setItem('authToken', token);
            console.log(' Token đã được lưu vào localStorage');

            // 2. Verify token đã lưu thành công
            const savedToken = localStorage.getItem('authToken');
            if (savedToken === token) {
              console.log(' Xác nhận: Token đã lưu thành công trong localStorage');
            } else {
              console.error(' Cảnh báo: Token không được lưu đúng!');
            }

            // 3. Giải mã Token để lấy thông tin user
            try {
              const decodedToken: any = jwtDecode(token);
              console.log('📦 Decoded Token:', decodedToken);

              const role = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decodedToken['role'];
              const email = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decodedToken['email'];
              const name = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || decodedToken['name'];

              console.log('🎭 ROLE:', role);
              console.log('📧 EMAIL:', email);
              console.log('👤 TÊN:', name);

              // Hiển thị thông báo cho user

              // 4. Điều hướng dựa trên Role
              if (role === 'HR' || role === 'ADMIN' || role === 'INTERVIEWER') {
                console.log('➡️ Chuyển hướng đến HR Dashboard...');
                this.router.navigate(['/hr/dashboard']);
              } else {
                console.log('➡️ Chuyển hướng đến Candidate Home...');
                this.router.navigate(['/candidate/home']);
              }

            } catch (error) {
              console.error(' Lỗi giải mã token:', error);
              this.toast.warning('Đăng nhập thành công', 'Nhưng không thể đọc thông tin user. Vui lòng thử lại.');
              this.router.navigate(['/']);
            }
          },
          error: (err) => {
            console.error(err);
            this.toast.error('Đăng nhập thất bại', 'Kiểm tra lại email hoặc mật khẩu.');
          }
        });
    } else {
      this.loginForm.markAllAsTouched();
    }
  }
}