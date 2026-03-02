import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChatWidget } from './chat-widget';

describe('ChatWidget', () => {
  let component: ChatWidget;
  let fixture: ComponentFixture<ChatWidget>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatWidget]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChatWidget);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
