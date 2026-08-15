import { api } from "./api";
import type {
  CardTask,
  CreateCardTaskRequest,
  UpdateCardTaskRequest,
} from "../types/cardTask";

export async function getCardTasks(cardId: string): Promise<CardTask[]> {
  const response = await api.get<CardTask[]>(`/cards/${cardId}/tasks`);
  return response.data;
}

export async function createCardTask(
  cardId: string,
  request: CreateCardTaskRequest
): Promise<CardTask> {
  const response = await api.post<CardTask>(`/cards/${cardId}/tasks`, request);
  return response.data;
}

export async function markCardTaskAsCompleted(taskId: string): Promise<void> {
  await api.patch(`/cardtasks/${taskId}/complete`);
}

export async function markCardTaskAsPending(taskId: string): Promise<void> {
  await api.patch(`/cardtasks/${taskId}/pending`);
}

export async function deleteCardTask(taskId: string): Promise<void> {
  await api.delete(`/cardtasks/${taskId}`);
}

export async function updateCardTask(
  taskId: string,
  request: UpdateCardTaskRequest
): Promise<void> {
  await api.patch(`/cardtasks/${taskId}`, request);
}
