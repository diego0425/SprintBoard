import { api } from "./api";
import type {
  Card,
  CreateCardRequest,
  UpdateCardRequest,
  UpdateCardStatusRequest,
} from "../types/card";

export async function createCard(
  boardId: string,
  request: CreateCardRequest
): Promise<Card> {
  const response = await api.post<Card>(`/boards/${boardId}/cards`, request);
  return response.data;
}

export async function changeCardStatus(
  cardId: string,
  request: UpdateCardStatusRequest
): Promise<void> {
  await api.patch(`/cards/${cardId}/status`, request);
}

export async function deleteCard(cardId: string): Promise<void> {
  await api.delete(`/cards/${cardId}`);
}

export async function updateCard(
  cardId: string,
  request: UpdateCardRequest
): Promise<void> {
  await api.patch(`/cards/${cardId}`, request);
}
