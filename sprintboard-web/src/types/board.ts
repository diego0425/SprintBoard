export const BoardRole = {
  Owner: 1,
  Admin: 2,
  Member: 3,
} as const;

export type BoardRole = (typeof BoardRole)[keyof typeof BoardRole];

export interface Board {
  id: string;
  name: string;
  ownerId: string;
  createdAt: string;
}

export interface BoardMember {
  userId: string;
  username: string;
  role: BoardRole;
  profileImageUrl: string | null;
}

export interface CreateBoardRequest {
  name: string;
}

export interface CreateBoardInvitationRequest {
  email: string;
}

export interface BoardInvitationResponse {
  id: string;
  boardId: string;
  email: string;
  token: string;
  expiresAt: string;
  createdAt: string;
}

export interface ChangeBoardMemberRoleRequest {
  memberUserId: string;
  newRole: BoardRole;
}

export interface UpdateBoardRequest {
  name?: string;
}
