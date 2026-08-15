import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import Header from "../../components/layout/Header";
import AsyncState from "../../components/ui/AsyncState";
import { createBoard, getBoards } from "../../services/boardService";
import type { Board } from "../../types/board";
import { useAuth } from "../../hooks/useAuth";

export default function BoardsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [boards, setBoards] = useState<Board[]>([]);
  const [boardName, setBoardName] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState("");

  const loadBoards = useCallback(async () => {
    try {
      setError("");
      setIsLoading(true);

      const boardList = await getBoards();
      setBoards(boardList);
    } catch (loadError) {
      console.error(loadError);
      setError("Failed to load boards.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadBoards();
  }, [loadBoards]);

  async function handleCreateBoard(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!boardName.trim() || isCreating) {
      return;
    }

    try {
      setError("");
      setIsCreating(true);

      const createdBoard = await createBoard({ name: boardName.trim() });

      setBoards((previous) => [createdBoard, ...previous]);
      setBoardName("");
    } catch (createError) {
      console.error(createError);
      setError("Failed to create board.");
    } finally {
      setIsCreating(false);
    }
  }

  return (
    <>
      <Header />

      <div className="boards-page">
        <header className="boards-header">
          <div>
            <h1>SprintBoard</h1>
            <p>Welcome{user?.username ? `, ${user.username}` : ""}.</p>
          </div>
        </header>

        <section className="boards-create-section">
          <form className="create-board-form" onSubmit={handleCreateBoard}>
            <input
              type="text"
              placeholder="New board name"
              value={boardName}
              onChange={(event) => setBoardName(event.target.value)}
              disabled={isCreating}
            />
            <button type="submit" disabled={isCreating || !boardName.trim()}>
              {isCreating ? "Creating..." : "Create board"}
            </button>
          </form>
        </section>

        {error && !isLoading && (
          <AsyncState type="error" message={error} onRetry={loadBoards} />
        )}

        {isLoading ? (
          <AsyncState type="loading" message="Loading boards..." />
        ) : !error && boards.length === 0 ? (
          <AsyncState
            type="empty"
            title="No boards yet"
            message="Create your first board to get started."
          />
        ) : !error ? (
          <section className="boards-grid">
            {boards.map((board) => (
              <article
                key={board.id}
                className="board-card"
                onClick={() => navigate(`/boards/${board.id}`)}
              >
                <h2>{board.name}</h2>
                <p>Created at: {new Date(board.createdAt).toLocaleDateString()}</p>
              </article>
            ))}
          </section>
        ) : null}
      </div>
    </>
  );
}
