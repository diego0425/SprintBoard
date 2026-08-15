import { useState } from "react";
import type { Card } from "../../types/card";
import CardTaskList from "./CardTaskList";
import ConfirmDialog from "../ui/ConfirmDialog";
import { Trash2, Pencil } from "lucide-react";

interface BoardCardProps {
  card: Card;
  onMove: (cardId: string, newStatus: number) => Promise<void>;
  onDelete: (cardId: string) => Promise<void>;
  onEdit: (card: Card) => void;
}

export default function BoardCard({ card, onMove, onDelete, onEdit }: BoardCardProps) {
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isMoving, setIsMoving] = useState(false);

  async function handleConfirmDelete() {
    if (isDeleting) return;

    try {
      setIsDeleting(true);
      await onDelete(card.id);
      setIsDeleteDialogOpen(false);
    } catch (deleteError) {
      console.error("Failed to delete card", deleteError);
    } finally {
      setIsDeleting(false);
    }
  }

  async function handleMove(newStatus: number) {
    if (isMoving || isDeleting) return;

    try {
      setIsMoving(true);
      await onMove(card.id, newStatus);
    } finally {
      setIsMoving(false);
    }
  }

  return (
    <>
      <article className="kanban-card">
        <div className="kanban-card-top">
          <h3>{card.title}</h3>

          <div className="kanban-card-actions-top">
            <button
              type="button"
              className="icon-edit-button"
              onClick={() => onEdit(card)}
              title="Edit card"
              disabled={isDeleting || isMoving}
            >
              <Pencil />
            </button>

            <button
              type="button"
              className="icon-delete-button"
              onClick={() => setIsDeleteDialogOpen(true)}
              title="Delete card"
              disabled={isDeleting || isMoving}
            >
              <Trash2 />
            </button>
          </div>
        </div>

        {card.description && <p>{card.description}</p>}

        <CardTaskList cardId={card.id} />

        <div className="kanban-card-actions">
          {card.status > 1 && (
            <button
              type="button"
              onClick={() => handleMove(card.status - 1)}
              disabled={isMoving || isDeleting}
            >
              {isMoving ? "Moving..." : "← Back"}
            </button>
          )}

          {card.status < 3 && (
            <button
              type="button"
              onClick={() => handleMove(card.status + 1)}
              disabled={isMoving || isDeleting}
            >
              {isMoving ? "Moving..." : "Next →"}
            </button>
          )}
        </div>
      </article>

      <ConfirmDialog
        isOpen={isDeleteDialogOpen}
        title="Delete card"
        message={`Are you sure you want to delete "${card.title}"? Its checklist items will also be deleted.`}
        confirmText="Delete card"
        onConfirm={handleConfirmDelete}
        onCancel={() => setIsDeleteDialogOpen(false)}
        isLoading={isDeleting}
      />
    </>
  );
}
