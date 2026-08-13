export interface AdminAmenity {
  id: string;
  name: string;
  slug: string;
  iconKey: string | null;
  hotelCount: number;
}

export interface AdminAmenityUpsert {
  name: string;
  slug: string;
  iconKey: string | null;
}
