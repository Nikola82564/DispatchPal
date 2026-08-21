import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditDispatchRequest } from './edit-dispatch-request';

describe('EditDispatchRequest', () => {
  let component: EditDispatchRequest;
  let fixture: ComponentFixture<EditDispatchRequest>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditDispatchRequest],
    }).compileComponents();

    fixture = TestBed.createComponent(EditDispatchRequest);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
