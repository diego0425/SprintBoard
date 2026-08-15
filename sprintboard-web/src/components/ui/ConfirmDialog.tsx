import Modal from "./Modal";

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  loadingText?: string;
  onConfirm: () => void | Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

export default function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmText = "Delete",
  cancelText = "Cancel",
  loadingText = "Deleting...",
  onConfirm,
  onCancel,
  isLoading = false,
}: ConfirmDialogProps) {
  function handleCancel() {
    if (!isLoading) {
      onCancel();
    }
  }

  return (
    <Modal title={title} isOpen={isOpen} onClose={handleCancel} disableClose={isLoading}>
      <div className="confirm-dialog">
        <p>{message}</p>

        <div className="confirm-dialog-actions">
          <button
            type="button"
            className="confirm-cancel-button"
            onClick={handleCancel}
            disabled={isLoading}
          >
            {cancelText}
          </button>

          <button
            type="button"
            className="confirm-delete-button"
            onClick={onConfirm}
            disabled={isLoading}
          >
            {isLoading ? loadingText : confirmText}
          </button>
        </div>
      </div>
    </Modal>
  );
}
