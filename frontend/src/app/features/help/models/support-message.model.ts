export interface CreateSupportMessageRequest {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export interface SupportMessageConfirmation {
  id: string;
  createdAtUtc: string;
}
