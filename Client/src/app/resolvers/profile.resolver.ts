import { inject } from '@angular/core';
import { ResolveFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CandidateService, CandidateProfileDto } from '../services/candidate.service';

export const profileResolver: ResolveFn<CandidateProfileDto | null> = (
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
) => {
    const candidateService = inject(CandidateService);

    return candidateService.getProfile().pipe(
        catchError(error => {
            console.error('Resolver failed to load profile', error);
            return of(null); // Return null on error to allow component to handle fallback
        })
    );
};
