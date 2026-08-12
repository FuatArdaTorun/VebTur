export interface HotelSummary {
  id: string;
  name: string;
  slug: string;
  city: string;
  country: string;
  starRating: number;
  thumbnailUrl: string | null;
  startingNightlyPrice: number | null;
  currency: string | null;
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
  starRating: number;
  officialWebsiteUrl: string | null;
  images: HotelImage[];
  amenities: Amenity[];
  roomTypes: RoomType[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
