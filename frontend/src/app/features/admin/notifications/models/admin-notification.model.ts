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

export type AdminNotificationSort =
  | 'created-desc'
  | 'sent-asc'
  | 'sent-desc'
  | 'reference-asc'
  | 'reference-desc'
  | 'recipient-asc'
  | 'recipient-desc'
  | 'subject-asc'
  | 'subject-desc';

export interface AdminNotificationListParams {
  search?: string;
  sort?: AdminNotificationSort;
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
