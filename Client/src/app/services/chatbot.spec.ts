import { TestBed } from '@angular/core/testing';

import { Chatbot } from './chatbot';

describe('Chatbot', () => {
  let service: Chatbot;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Chatbot);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
