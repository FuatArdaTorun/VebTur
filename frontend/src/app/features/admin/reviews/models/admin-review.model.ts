export interface AdminReviewSummary {
  id: string;
  hotelId: string;
  hotelName: string;
  reviewerName: string;
  rating: number;
  comment: string | null;
  roomTypeName: string;
  checkInDate: string;
  checkOutDate: string;
  isHidden: boolean;
  createdAtUtc: string;
}

export type AdminReviewSort =
  | 'created-desc'
  | 'hotel-asc'
  | 'hotel-desc'
  | 'reviewer-asc'
  | 'reviewer-desc'
  | 'rating-asc'
  | 'rating-desc'
  | 'status-asc'
  | 'status-desc';

export interface AdminReviewListParams {
  search?: string;
  sort?: AdminReviewSort;
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
