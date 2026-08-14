import { ReservationStatus } from '../../../reservations/models/reservation.model';

export interface AdminReservationSummary {
  id: string;
  referenceNumber: string;
  hotelName: string;
  guestFullName: string;
  checkInDate: string;
  checkOutDate: string;
  status: ReservationStatus;
  createdAtUtc: string;
}

export interface AdminReservationDetail {
  id: string;
  referenceNumber: string;
  hotelId: string;
  hotelName: string;
  roomTypeId: string;
  roomTypeName: string;
  roomTypeAvailableCount: number;
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

export type AdminReservationSort =
  | 'created-desc'
  | 'hotel-asc'
  | 'hotel-desc'
  | 'checkin-asc'
  | 'checkin-desc'
  | 'status-asc'
  | 'status-desc';

export interface AdminReservationListParams {
  status?: ReservationStatus | ReservationStatus[];
  hotelId?: string;
  search?: string;
  sort?: AdminReservationSort;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
