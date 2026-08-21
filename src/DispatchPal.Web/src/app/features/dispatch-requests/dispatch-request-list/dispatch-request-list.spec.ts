import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DispatchRequestList } from './dispatch-request-list';

describe('DispatchRequestList', () => {
  let component: DispatchRequestList;
  let fixture: ComponentFixture<DispatchRequestList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DispatchRequestList],
    }).compileComponents();

    fixture = TestBed.createComponent(DispatchRequestList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
