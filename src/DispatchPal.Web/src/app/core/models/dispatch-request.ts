export interface DispatchRequestStatusHistory {
  id: string;
  status: string;
  description: string;
  changedAtUtc: string;
}

export interface DispatchRequest {
  id: string;
  customerId: string;
  customerName: string;
  pickupAddress: string;
  deliveryAddress: string;
  packageDescription: string;
  status: string;
  createdAtUtc: string;
  statusHistory: DispatchRequestStatusHistory[];
}

export interface CreateDispatchRequest {
  customerId: string;
  pickupAddress: string;
  deliveryAddress: string;
  packageDescription: string;
}