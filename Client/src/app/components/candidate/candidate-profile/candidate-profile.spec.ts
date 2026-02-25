import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CandidateProfile } from './candidate-profile';

describe('CandidateProfile', () => {
  let component: CandidateProfile;
  let fixture: ComponentFixture<CandidateProfile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateProfile]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CandidateProfile);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
