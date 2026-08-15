import { api } from "./api";
import type { Card } from "../types/card";
import type {
  Board,
  BoardInvitationResponse,
  BoardMember,
  ChangeBoardMemberRoleRequest,
  CreateBoardInvitationRequest,
  CreateBoardRequest,
  UpdateBoardRequest,
} from "../types/board";

export async function getBoards(): Promise<Board[]> {
  const response = await api.get<Board[]>("/boards");
  return response.data;
}

export async function getBoardById(boardId: string): Promise<Board> {
  const response = await api.get<Board>(`/boards/${boardId}`);
  return response.data;
}

export async function createBoard(request: CreateBoardRequest): Promise<Board> {
  const response = await api.post<Board>("/boards", request);
  return response.data;
}

export async function getBoardCards(boardId: string): Promise<Card[]> {
  const response = await api.get<Card[]>(`/boards/${boardId}/cards`);
  return response.data;
}

export async function getBoardMembers(boardId: string): Promise<BoardMember[]> {
  const response = await api.get<BoardMember[]>(`/boards/${boardId}/members`);
  return response.data;
}

export async function inviteMemberToBoard(
  boardId: string,
  request: CreateBoardInvitationRequest
): Promise<BoardInvitationResponse> {
  const response = await api.post<BoardInvitationResponse>(
    `/boards/${boardId}/invitations`,
    request
  );

  return response.data;
}

export async function changeBoardMemberRole(
  boardId: string,
  request: ChangeBoardMemberRoleRequest
): Promise<void> {
  await api.patch(`/boards/${boardId}/members/role`, request);
}

export async function removeBoardMember(
  boardId: string,
  memberUserId: string
): Promise<void> {
  await api.delete(`/boards/${boardId}/members/${memberUserId}`);
}

export async function leaveBoard(boardId: string): Promise<void> {
  await api.delete(`/boards/${boardId}/members/me`);
}

export async function deleteBoard(boardId: string): Promise<void> {
  await api.delete(`/boards/${boardId}`);
}

export async function updateBoard(
  boardId: string,
  request: UpdateBoardRequest
): Promise<void> {
  await api.patch(`/boards/${boardId}`, request);
}
