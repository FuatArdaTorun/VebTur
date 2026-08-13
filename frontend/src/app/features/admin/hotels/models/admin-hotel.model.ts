export interface AdminHotelImage {
  id: string | null;
  url: string;
  altText: string | null;
  displayOrder: number;
}

export interface AdminRoomType {
  id: string | null;
  name: string;
  description: string;
  capacity: number;
  baseNightlyPrice: number;
  currency: string;
  isActive: boolean;
}

export interface AdminHotelSupervisor {
  id: string | null;
  fullName: string;
  email: string;
  isActive: boolean;
}

export interface AdminHotelSummary {
  id: string;
  name: string;
  slug: string;
  city: string;
  isActive: boolean;
  thumbnailUrl: string | null;
  updatedAtUtc: string;
}

export interface AdminHotelDetail {
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
  officialWebsiteUrl: string | null;
  googlePlaceId: string | null;
  phoneNumber: string | null;
  googleRating: number | null;
  googleRatingCount: number | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  images: AdminHotelImage[];
  roomTypes: AdminRoomType[];
  supervisors: AdminHotelSupervisor[];
  amenitySlugs: string[];
}

/** POST/PUT body — same shape as AdminHotelDetail minus the server-owned identity/audit fields. */
export type AdminHotelUpsert = Omit<AdminHotelDetail, 'id' | 'createdAtUtc' | 'updatedAtUtc'>;

export interface AdminHotelListParams {
  search?: string;
  isActive?: boolean;
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
