import { useCallback, useEffect, useState } from "react";
import { Trash2 } from "lucide-react";
import {
  createCardTask,
  deleteCardTask,
  getCardTasks,
  markCardTaskAsCompleted,
  markCardTaskAsPending,
} from "../../services/cardTaskService";
import type { CardTask } from "../../types/cardTask";
import ConfirmDialog from "../ui/ConfirmDialog";
import AsyncState from "../ui/AsyncState";

interface CardTaskListProps {
  cardId: string;
}

export default function CardTaskList({ cardId }: CardTaskListProps) {
  const [tasks, setTasks] = useState<CardTask[]>([]);
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [updatingTaskId, setUpdatingTaskId] = useState<string | null>(null);
  const [taskPendingDelete, setTaskPendingDelete] = useState<CardTask | null>(null);
  const [isDeletingTask, setIsDeletingTask] = useState(false);
  const [error, setError] = useState("");

  const loadTasks = useCallback(async () => {
    try {
      setError("");
      setIsLoading(true);
      const cardTasks = await getCardTasks(cardId);
      setTasks(cardTasks);
    } catch (loadError) {
      console.error("Failed to load card tasks", loadError);
      setError("Failed to load checklist.");
    } finally {
      setIsLoading(false);
    }
  }, [cardId]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  async function handleCreateTask(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!newTaskTitle.trim() || isCreating) {
      return;
    }

    try {
      setError("");
      setIsCreating(true);

      const createdTask = await createCardTask(cardId, {
        title: newTaskTitle.trim(),
        position: tasks.length,
      });

      setTasks((currentTasks) => [...currentTasks, createdTask]);
      setNewTaskTitle("");
    } catch (createError) {
      console.error("Failed to create card task", createError);
      setError("Failed to add checklist item.");
    } finally {
      setIsCreating(false);
    }
  }

  async function handleToggleTask(task: CardTask) {
    if (updatingTaskId || isDeletingTask) return;

    try {
      setError("");
      setUpdatingTaskId(task.id);

      if (task.isCompleted) {
        await markCardTaskAsPending(task.id);
      } else {
        await markCardTaskAsCompleted(task.id);
      }

      setTasks((currentTasks) =>
        currentTasks.map((currentTask) =>
          currentTask.id === task.id
            ? { ...currentTask, isCompleted: !currentTask.isCompleted }
            : currentTask
        )
      );
    } catch (updateError) {
      console.error("Failed to update task status", updateError);
      setError("Failed to update checklist item.");
    } finally {
      setUpdatingTaskId(null);
    }
  }

  async function handleConfirmDeleteTask() {
    if (!taskPendingDelete || isDeletingTask) return;

    try {
      setError("");
      setIsDeletingTask(true);
      await deleteCardTask(taskPendingDelete.id);
      setTasks((currentTasks) =>
        currentTasks.filter((task) => task.id !== taskPendingDelete.id)
      );
      setTaskPendingDelete(null);
    } catch (deleteError) {
      console.error("Failed to delete task", deleteError);
      setError("Failed to delete checklist item.");
    } finally {
      setIsDeletingTask(false);
    }
  }

  const completedTaskCount = tasks.filter((task) => task.isCompleted).length;

  return (
    <div className="card-task-list">
      <h4>
        Checklist ({completedTaskCount}/{tasks.length})
      </h4>

      <form className="card-task-form" onSubmit={handleCreateTask}>
        <input
          type="text"
          placeholder="Add a task"
          value={newTaskTitle}
          onChange={(event) => setNewTaskTitle(event.target.value)}
          disabled={isCreating}
        />
        <button type="submit" disabled={isCreating || !newTaskTitle.trim()}>
          {isCreating ? "..." : "Add"}
        </button>
      </form>

      {isLoading ? (
        <AsyncState type="loading" message="Loading checklist..." compact />
      ) : error && tasks.length === 0 ? (
        <AsyncState type="error" message={error} onRetry={loadTasks} compact />
      ) : tasks.length === 0 ? (
        <AsyncState type="empty" message="No checklist items yet." compact />
      ) : (
        <>
          {error && <p className="card-task-error">{error}</p>}
          <ul className="card-task-items">
            {tasks.map((task) => {
              const isUpdating = updatingTaskId === task.id;

              return (
                <li key={task.id} className="card-task-item">
                  <div className="card-task-row">
                    <label className="card-task-label">
                      <input
                        type="checkbox"
                        checked={task.isCompleted}
                        onChange={() => handleToggleTask(task)}
                        disabled={Boolean(updatingTaskId) || isDeletingTask}
                      />
                      <span className={task.isCompleted ? "completed" : ""}>
                        {task.title}{isUpdating ? "..." : ""}
                      </span>
                    </label>

                    <button
                      type="button"
                      className="icon-delete-button"
                      onClick={() => setTaskPendingDelete(task)}
                      title="Delete task"
                      disabled={Boolean(updatingTaskId) || isDeletingTask}
                    >
                      <Trash2 />
                    </button>
                  </div>
                </li>
              );
            })}
          </ul>
        </>
      )}

      <ConfirmDialog
        isOpen={Boolean(taskPendingDelete)}
        title="Delete checklist item"
        message={`Are you sure you want to delete "${taskPendingDelete?.title ?? "this item"}"?`}
        confirmText="Delete item"
        onConfirm={handleConfirmDeleteTask}
        onCancel={() => setTaskPendingDelete(null)}
        isLoading={isDeletingTask}
      />
    </div>
  );
}
