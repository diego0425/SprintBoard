import { api } from "./api";
import type { MeResponse, UpdateUserRequest } from "../types/user";

export async function getMe(): Promise<MeResponse> {
  const response = await api.get<MeResponse>("/users/me");
  return response.data;
}

export async function updateMe(request: UpdateUserRequest): Promise<void> {
  await api.patch("/users/me", request);
}

export async function updateProfileImage(
  file: File
): Promise<{ profileImageUrl: string }> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await api.patch<{ profileImageUrl: string }>(
    "/users/me/profile-image",
    formData,
    {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    }
  );

  return response.data;
}
