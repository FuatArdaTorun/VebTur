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

export interface AdminSupportMessageListParams {
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
