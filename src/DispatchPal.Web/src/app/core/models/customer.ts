export interface Customer {
  id: string;
  name: string;
  email: string;
  phone: string;
  createdAtUtc: string;
}

export interface CreateCustomer {
  name: string;
  email: string;
  phone: string;
}