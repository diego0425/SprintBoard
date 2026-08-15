import { api } from "./api";
import type { RespondToInvitationRequest } from "../types/invitation";

export async function acceptInvitation(
  data: RespondToInvitationRequest
): Promise<void> {
  await api.post("/invitations/accept", data);
}

export async function declineInvitation(
  data: RespondToInvitationRequest
): Promise<void> {
  await api.post("/invitations/decline", data);
}