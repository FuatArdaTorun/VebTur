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

export interface AdminReviewListParams {
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
