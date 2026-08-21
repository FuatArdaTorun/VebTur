export interface AdminSupportMessage {
  id: string;
  senderName: string;
  senderEmail: string;
  subject: string;
  message: string;
  replyMessage: string | null;
  repliedAtUtc: string | null;
  createdAtUtc: string;
}

export type AdminSupportMessageSort =
  | 'received-desc'
  | 'received-asc'
  | 'sender-asc'
  | 'sender-desc'
  | 'subject-asc'
  | 'subject-desc'
  | 'status-asc'
  | 'status-desc';

export interface AdminSupportMessageListParams {
  search?: string;
  sort?: AdminSupportMessageSort;
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
