export interface AdminNotificationLog {
  id: string;
  reservationRequestId: string;
  reservationReferenceNumber: string;
  hotelName: string;
  type: string;
  recipient: string;
  subject: string;
  createdAtUtc: string;
  sentAtUtc: string | null;
  errorMessage: string | null;
}

export interface AdminNotificationListParams {
  search?: string;
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
