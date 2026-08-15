export interface UpdateUserRequest {
  fullName?: string;
  username?: string;
  oldPassword?: string;
  newPassword?: string;
}

export interface MeResponse {
  id: string;
  username: string;
  fullName: string;
  email: string;
  profileImageUrl?: string | null;
}
