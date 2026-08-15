export interface Card {
  id: string;
  boardId: string;
  title: string;
  description?: string;
  status: number;
  position: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCardRequest {
  title: string;
  description?: string;
  position?: number;
}

export interface UpdateCardStatusRequest {
  status: number;
}

export interface UpdateCardRequest {
  title: string;
  description?: string;
}