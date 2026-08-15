import type { Card } from "../../types/card";
import BoardCard from "./BoardCard";

interface BoardColumnProps {
  title: string;
  cards: Card[];
  onMove: (cardId: string, newStatus: number) => Promise<void>;
  onDelete: (cardId: string) => Promise<void>;
  onEdit: (card: Card) => void;
}

export default function BoardColumn({
  title,
  cards,
  onMove,
  onDelete,
  onEdit,
}: BoardColumnProps) {
  return (
    <section className="kanban-column">
      <header className="kanban-column-header">
        <h2>{title}</h2>
        <span>{cards.length}</span>
      </header>

      <div className="kanban-column-cards">
        {cards.length === 0 ? (
          <p className="kanban-empty">No cards here yet.</p>
        ) : (
          cards.map((card) => (
            <BoardCard
              key={card.id}
              card={card}
              onMove={onMove}
              onDelete={onDelete}
              onEdit={onEdit}
            />
          ))
        )}
      </div>
    </section>
  );
}
