export interface HotelSummary {
  id: string;
  name: string;
  slug: string;
  city: string;
  country: string;
  starRating: number | null;
  googleRating: number | null;
  googleRatingCount: number | null;
  thumbnailUrl: string | null;
  startingNightlyPrice: number | null;
  currency: string | null;
  customerRating: number | null;
  customerReviewCount: number;
}

export interface HotelImage {
  url: string;
  altText: string | null;
  displayOrder: number;
}

export interface Amenity {
  id: string;
  name: string;
  slug: string;
  iconKey: string | null;
}

export interface RoomType {
  id: string;
  name: string;
  description: string;
  capacity: number;
  baseNightlyPrice: number;
  currency: string;
  availableCount: number;
}

export interface HotelDetail {
  id: string;
  name: string;
  slug: string;
  description: string;
  city: string;
  country: string;
  address: string;
  latitude: number;
  longitude: number;
  starRating: number | null;
  googleRating: number | null;
  googleRatingCount: number | null;
  officialWebsiteUrl: string | null;
  phoneNumber: string | null;
  images: HotelImage[];
  amenities: Amenity[];
  roomTypes: RoomType[];
  customerRating: number | null;
  customerReviewCount: number;
}

export interface Review {
  id: string;
  rating: number;
  comment: string | null;
  reviewerName: string;
  roomTypeName: string;
  checkInDate: string;
  checkOutDate: string;
  createdAtUtc: string;
}

/** One of the current customer's own Confirmed reservations at this hotel that doesn't have a review yet. */
export interface ReviewableReservation {
  reservationRequestId: string;
  roomTypeName: string;
  checkInDate: string;
  checkOutDate: string;
}

export interface HotelReviewsResponse {
  reviews: Review[];
  myReviewableReservations: ReviewableReservation[];
}

export interface CreateReviewRequest {
  reservationRequestId: string;
  rating: number;
  comment: string | null;
}

export interface ExternalRating {
  provider: string;
  rating: number;
  maximumRating: number;
  reviewCount: number;
  isDemoData: boolean;
  lastUpdatedAtUtc: string;
}

export interface RoomTypeAvailability {
  fullyBookedDates: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type HotelSortOrder = 'recommended' | 'price-asc' | 'price-desc' | 'star-desc';

export interface HotelSearchParams {
  city?: string;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  minStarRating?: number;
  amenities?: string[];
  minCapacity?: number;
  sort?: HotelSortOrder;
  page: number;
  pageSize: number;
}
