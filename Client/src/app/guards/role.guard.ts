import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { isPlatformBrowser } from '@angular/common';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const platformId = inject(PLATFORM_ID);

    console.log('🔒 roleGuard running...', isPlatformBrowser(platformId) ? 'BROWSER' : 'SERVER');

    // Get required roles from route data
    const requiredRoles = route.data['roles'] as string[];

    if (!requiredRoles || requiredRoles.length === 0) {
        console.log('   No role restriction');
        return true; // No role restriction
    }

    console.log('  Required roles:', requiredRoles);

    // ⚡ SSR Fix: Allow navigation on server, check on client
    if (!isPlatformBrowser(platformId)) {
        console.log('  ⏭️  Running on SERVER - allowing navigation (will check on client)');
        return true; // Let it through on server, will check on client
    }

    // Check localStorage token directly
    const token = typeof window !== 'undefined' ? localStorage.getItem('authToken') : null;
    console.log('  Token exists?', !!token);

    // Check if user is authenticated
    const isAuth = authService.isAuthenticated();
    console.log('  isAuthenticated?', isAuth);

    if (!isAuth) {
        console.warn('   Not authenticated, redirecting to /login');
        router.navigate(['/login']);
        return false;
    }

    // Get current user
    const currentUser = authService.getCurrentUser();
    console.log('  Current user:', currentUser ? `${currentUser.name} (${currentUser.role})` : 'null');

    if (!currentUser || !currentUser.role) {
        console.warn('   No user/role, redirecting to /login');
        router.navigate(['/login']);
        return false;
    }

    // Check if user has required role
    const hasRequiredRole = requiredRoles.includes(currentUser.role);
    console.log('  Has required role?', hasRequiredRole);

    if (!hasRequiredRole) {
        console.warn(`   Access denied. Required: ${requiredRoles.join(', ')}, Got: ${currentUser.role}`);
        router.navigate(['/403']);
        return false;
    }

    console.log('   Access granted');
    return true;
};
