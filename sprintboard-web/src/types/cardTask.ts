export interface CardTask {
  id: string;
  cardId: string;
  title: string;
  isCompleted: boolean;
  position: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCardTaskRequest {
  title: string;
  position?: number;
}

export interface UpdateCardTaskRequest {
  title: string;
}