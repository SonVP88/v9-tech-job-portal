import { TestBed } from '@angular/core/testing';

import { PublicJob } from './public-job';

describe('PublicJob', () => {
  let service: PublicJob;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PublicJob);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
