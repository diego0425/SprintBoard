import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Pencil, Trash2, Users } from "lucide-react";

import {
  deleteBoard,
  getBoardById,
  getBoardCards,
  getBoardMembers,
  updateBoard,
} from "../../services/boardService";
import { changeCardStatus, createCard, deleteCard, updateCard } from "../../services/cardService";
import { BoardRole, type Board } from "../../types/board";
import type { Card } from "../../types/card";
import BoardColumn from "../../components/board/BoardColumn";
import InviteMemberForm from "../../components/board/InviteMemberForm";
import MembersDrawer from "../../components/board/MembersDrawer";
import Modal from "../../components/ui/Modal";
import ConfirmDialog from "../../components/ui/ConfirmDialog";
import AsyncState from "../../components/ui/AsyncState";
import Header from "../../components/layout/Header";
import BackButton from "../../components/common/BackButton";
import { useAuth } from "../../hooks/useAuth";

export default function BoardDetailsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { boardId } = useParams<{ boardId: string }>();

  const [board, setBoard] = useState<Board | null>(null);
  const [cards, setCards] = useState<Card[]>([]);
  const [currentUserRole, setCurrentUserRole] = useState<BoardRole | null>(null);
  const [newCardTitle, setNewCardTitle] = useState("");
  const [newCardDescription, setNewCardDescription] = useState("");

  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState("");

  const [isCreateCardModalOpen, setIsCreateCardModalOpen] = useState(false);
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isMembersDrawerOpen, setIsMembersDrawerOpen] = useState(false);
  const [isDeleteBoardDialogOpen, setIsDeleteBoardDialogOpen] = useState(false);
  const [isDeletingBoard, setIsDeletingBoard] = useState(false);

  const [isEditBoardModalOpen, setIsEditBoardModalOpen] = useState(false);
  const [isEditCardModalOpen, setIsEditCardModalOpen] = useState(false);
  const [editingCard, setEditingCard] = useState<Card | null>(null);

  const [editedBoardName, setEditedBoardName] = useState("");
  const [editedCardTitle, setEditedCardTitle] = useState("");
  const [editedCardDescription, setEditedCardDescription] = useState("");

  const [isUpdatingBoard, setIsUpdatingBoard] = useState(false);
  const [isUpdatingCard, setIsUpdatingCard] = useState(false);

  const loadBoardData = useCallback(async () => {
    if (!boardId) {
      setError("Board identifier is missing.");
      setIsLoading(false);
      return;
    }

    try {
      setError("");
      setIsLoading(true);

      const [boardData, cardsData, membersData] = await Promise.all([
        getBoardById(boardId),
        getBoardCards(boardId),
        getBoardMembers(boardId),
      ]);

      const currentMember = membersData.find(
        (member) => member.userId === user?.userId
      );

      setBoard(boardData);
      setCards(cardsData);
      setCurrentUserRole(
        currentMember?.role ??
          (boardData.ownerId === user?.userId ? BoardRole.Owner : null)
      );
    } catch (loadError) {
      console.error(loadError);
      setError("Failed to load board details.");
    } finally {
      setIsLoading(false);
    }
  }, [boardId, user?.userId]);

  useEffect(() => {
    loadBoardData();
  }, [loadBoardData]);

  function openEditBoardModal() {
    if (!board) return;

    setEditedBoardName(board.name);
    setIsEditBoardModalOpen(true);
  }

  async function handleUpdateBoard(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!boardId || !editedBoardName.trim() || !board || isUpdatingBoard) return;

    try {
      setError("");
      setIsUpdatingBoard(true);

      await updateBoard(boardId, { name: editedBoardName.trim() });

      setBoard((previous) =>
        previous ? { ...previous, name: editedBoardName.trim() } : previous
      );

      setIsEditBoardModalOpen(false);
    } catch (updateBoardError) {
      console.error(updateBoardError);
      setError("Failed to update board.");
    } finally {
      setIsUpdatingBoard(false);
    }
  }

  function openEditCardModal(card: Card) {
    setEditingCard(card);
    setEditedCardTitle(card.title);
    setEditedCardDescription(card.description ?? "");
    setIsEditCardModalOpen(true);
  }

  async function handleUpdateCard(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!editingCard || !editedCardTitle.trim() || isUpdatingCard) return;

    try {
      setError("");
      setIsUpdatingCard(true);

      await updateCard(editingCard.id, {
        title: editedCardTitle.trim(),
        description: editedCardDescription.trim(),
      });

      setCards((previous) =>
        previous.map((card) =>
          card.id === editingCard.id
            ? {
                ...card,
                title: editedCardTitle.trim(),
                description: editedCardDescription.trim() || undefined,
              }
            : card
        )
      );

      setIsEditCardModalOpen(false);
      setEditingCard(null);
    } catch (updateCardError) {
      console.error(updateCardError);
      setError("Failed to update card.");
    } finally {
      setIsUpdatingCard(false);
    }
  }

  const todoCards = useMemo(() => cards.filter((card) => card.status === 1), [cards]);
  const doingCards = useMemo(() => cards.filter((card) => card.status === 2), [cards]);
  const doneCards = useMemo(() => cards.filter((card) => card.status === 3), [cards]);

  async function handleCreateCard(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!boardId || !newCardTitle.trim() || isCreating) return;

    try {
      setError("");
      setIsCreating(true);

      const newCard = await createCard(boardId, {
        title: newCardTitle.trim(),
        description: newCardDescription.trim() || undefined,
        position: cards.length,
      });

      setCards((previous) => [...previous, newCard]);
      setNewCardTitle("");
      setNewCardDescription("");
      setIsCreateCardModalOpen(false);
    } catch (createCardError) {
      console.error(createCardError);
      setError("Failed to create card.");
    } finally {
      setIsCreating(false);
    }
  }

  async function handleMoveCard(cardId: string, newStatus: number) {
    try {
      setError("");
      await changeCardStatus(cardId, { status: newStatus });

      setCards((previous) =>
        previous.map((card) =>
          card.id === cardId ? { ...card, status: newStatus } : card
        )
      );
    } catch (moveCardError) {
      console.error(moveCardError);
      setError("Failed to update card status.");
      throw moveCardError;
    }
  }

  async function handleDeleteCard(cardId: string) {
    try {
      setError("");
      await deleteCard(cardId);
      setCards((previous) => previous.filter((card) => card.id !== cardId));
    } catch (deleteCardError) {
      console.error(deleteCardError);
      setError("Failed to delete card.");
      throw deleteCardError;
    }
  }

  async function handleDeleteBoard() {
    if (!boardId || isDeletingBoard) return;

    try {
      setError("");
      setIsDeletingBoard(true);
      await deleteBoard(boardId);
      setIsDeleteBoardDialogOpen(false);
      navigate("/boards");
    } catch (deleteBoardError) {
      console.error(deleteBoardError);
      setIsDeleteBoardDialogOpen(false);
      setError("Failed to delete board.");
    } finally {
      setIsDeletingBoard(false);
    }
  }

  if (isLoading) {
    return (
      <>
        <Header />
        <main className="page-state-container">
          <AsyncState type="loading" message="Loading board..." />
        </main>
      </>
    );
  }

  if (error && !board) {
    return (
      <>
        <Header />
        <main className="page-state-container">
          <AsyncState
            type="error"
            title="Unable to open board"
            message={error}
            onRetry={loadBoardData}
          />
        </main>
      </>
    );
  }

  if (!board) {
    return (
      <>
        <Header />
        <main className="page-state-container">
          <AsyncState type="empty" title="Board not found" message="This board is unavailable." />
        </main>
      </>
    );
  }

  const isOwner =
    currentUserRole === BoardRole.Owner || board.ownerId === user?.userId;
  const canInviteMembers =
    isOwner || currentUserRole === BoardRole.Admin;

  return (
    <>
      <Header />

      <div className="page-back-row">
        <BackButton />
      </div>
      <div className="board-details-page">
        <header className="board-details-header">
          <div className="board-details-title-group">
            <div className="board-details-title-row">
              <h1>{board.name}</h1>

              {isOwner && (
                <>
                  <button
                    type="button"
                    className="icon-edit-button"
                    onClick={openEditBoardModal}
                    disabled={isDeletingBoard}
                    title="Edit board"
                  >
                    <Pencil />
                  </button>

                  <button
                    type="button"
                    className="icon-delete-button"
                    onClick={() => setIsDeleteBoardDialogOpen(true)}
                    title="Delete board"
                    disabled={isDeletingBoard}
                  >
                    <Trash2 />
                  </button>
                </>
              )}
            </div>

            <p>Manage your cards and workflow.</p>
          </div>

          <div className="board-details-actions">
            <button
              type="button"
              className="members-button"
              onClick={() => setIsMembersDrawerOpen(true)}
            >
              <Users size={17} />
              Members
            </button>

            {canInviteMembers && (
              <button type="button" onClick={() => setIsInviteModalOpen(true)}>
                Invite member
              </button>
            )}

            <button type="button" onClick={() => setIsCreateCardModalOpen(true)}>
              Create card
            </button>
          </div>
        </header>

        {error && <p className="page-inline-error">{error}</p>}

        <section className="kanban-board">
          <BoardColumn title="To Do" cards={todoCards} onMove={handleMoveCard} onDelete={handleDeleteCard} onEdit={openEditCardModal} />
          <BoardColumn title="Doing" cards={doingCards} onMove={handleMoveCard} onDelete={handleDeleteCard} onEdit={openEditCardModal} />
          <BoardColumn title="Done" cards={doneCards} onMove={handleMoveCard} onDelete={handleDeleteCard} onEdit={openEditCardModal} />
        </section>

        <Modal title="Create new card" isOpen={isCreateCardModalOpen} onClose={() => setIsCreateCardModalOpen(false)} disableClose={isCreating}>
          <form className="create-card-form" onSubmit={handleCreateCard}>
            <input
              type="text"
              placeholder="Card title"
              value={newCardTitle}
              onChange={(event) => setNewCardTitle(event.target.value)}
              disabled={isCreating}
            />
            <textarea
              placeholder="Description (optional)"
              value={newCardDescription}
              onChange={(event) => setNewCardDescription(event.target.value)}
              rows={3}
              disabled={isCreating}
            />
            <button type="submit" disabled={isCreating || !newCardTitle.trim()}>
              {isCreating ? "Creating..." : "Create card"}
            </button>
          </form>
        </Modal>

        {canInviteMembers && (
          <Modal
            title="Invite member"
            isOpen={isInviteModalOpen}
            onClose={() => setIsInviteModalOpen(false)}
          >
            {boardId && (
              <InviteMemberForm
                boardId={boardId}
                onSuccess={() => setIsInviteModalOpen(false)}
              />
            )}
          </Modal>
        )}

        {isOwner && (
          <Modal
            title="Edit board"
            isOpen={isEditBoardModalOpen}
            onClose={() => setIsEditBoardModalOpen(false)}
            disableClose={isUpdatingBoard}
          >
            <form className="create-card-form" onSubmit={handleUpdateBoard}>
              <input
                type="text"
                placeholder="Board name"
                value={editedBoardName}
                onChange={(event) => setEditedBoardName(event.target.value)}
                disabled={isUpdatingBoard}
              />
              <button
                type="submit"
                disabled={isUpdatingBoard || !editedBoardName.trim()}
              >
                {isUpdatingBoard ? "Saving..." : "Save changes"}
              </button>
            </form>
          </Modal>
        )}

        <Modal
          title="Edit card"
          isOpen={isEditCardModalOpen}
          onClose={() => {
            setIsEditCardModalOpen(false);
            setEditingCard(null);
          }}
          disableClose={isUpdatingCard}
        >
          <form className="create-card-form" onSubmit={handleUpdateCard}>
            <input
              type="text"
              placeholder="Card title"
              value={editedCardTitle}
              onChange={(event) => setEditedCardTitle(event.target.value)}
              disabled={isUpdatingCard}
            />
            <textarea
              placeholder="Description"
              value={editedCardDescription}
              onChange={(event) => setEditedCardDescription(event.target.value)}
              rows={3}
              disabled={isUpdatingCard}
            />
            <button type="submit" disabled={isUpdatingCard || !editedCardTitle.trim()}>
              {isUpdatingCard ? "Saving..." : "Save changes"}
            </button>
          </form>
        </Modal>

        {boardId && (
          <MembersDrawer
            boardId={boardId}
            currentUserId={user?.userId}
            currentUserRole={currentUserRole}
            isOpen={isMembersDrawerOpen}
            onClose={() => setIsMembersDrawerOpen(false)}
            onBoardLeft={() => navigate("/boards")}
          />
        )}

        {isOwner && (
          <ConfirmDialog
            isOpen={isDeleteBoardDialogOpen}
            title="Delete board"
            message={`Are you sure you want to delete "${board.name}"? All cards and checklist items will also be deleted.`}
            confirmText="Delete board"
            onConfirm={handleDeleteBoard}
            onCancel={() => setIsDeleteBoardDialogOpen(false)}
            isLoading={isDeletingBoard}
          />
        )}
      </div>
    </>
  );
}
