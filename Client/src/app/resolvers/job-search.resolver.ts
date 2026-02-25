import { inject } from '@angular/core';
import { ResolveFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { JobService, JobDto } from '../services/job.service';

export const jobResolver: ResolveFn<JobDto[]> = (
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
) => {
    const jobService = inject(JobService);

    const keyword = route.queryParams['keyword'];
    const location = route.queryParams['location'];
    // jobType/minSalary support can be added if needed by URL params

    return jobService.searchPublicJobs(keyword, location).pipe(
        catchError(error => {
            console.error('Resolver failed to load jobs', error);
            return of([]); // Return empty array on error to allow page load
        })
    );
};
