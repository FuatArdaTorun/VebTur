export type ReservationStatus = 'AwaitingApproval' | 'Confirmed' | 'Rejected' | 'Cancelled';

export interface CreateReservationRequest {
  hotelId: string;
  roomTypeId: string;
  guestFullName: string;
  guestEmail: string;
  guestPhone: string;
  checkInDate: string;
  checkOutDate: string;
  adultCount: number;
  childCount: number;
  specialRequests: string | null;
}

export interface UpdateReservationRequest {
  roomTypeId: string;
  checkInDate: string;
  checkOutDate: string;
  adultCount: number;
  childCount: number;
  specialRequests: string | null;
}

export interface ReservationRequestDetail {
  id: string;
  referenceNumber: string;
  hotelId: string;
  hotelName: string;
  roomTypeId: string;
  roomTypeName: string;
  guestFullName: string;
  guestEmail: string;
  guestPhone: string;
  checkInDate: string;
  checkOutDate: string;
  adultCount: number;
  childCount: number;
  specialRequests: string | null;
  estimatedPrice: number;
  currency: string;
  status: ReservationStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
