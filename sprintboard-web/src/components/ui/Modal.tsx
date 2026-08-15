import { useEffect, type ReactNode } from "react";
import { createPortal } from "react-dom";

interface ModalProps {
  title: string;
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
  disableClose?: boolean;
}

export default function Modal({
  title,
  isOpen,
  onClose,
  children,
  disableClose = false,
}: ModalProps) {
  useEffect(() => {
    function handleEsc(event: KeyboardEvent) {
      if (event.key === "Escape" && !disableClose) {
        onClose();
      }
    }

    if (isOpen) {
      window.addEventListener("keydown", handleEsc);
      document.body.style.overflow = "hidden";
    }

    return () => {
      window.removeEventListener("keydown", handleEsc);
      document.body.style.overflow = "";
    };
  }, [disableClose, isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  return createPortal(
    <div
      className="modal-overlay"
      onClick={() => {
        if (!disableClose) onClose();
      }}
    >
      <div
        className="modal-container"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="modal-header">
          <h2>{title}</h2>

          <button
            type="button"
            className="modal-close-button"
            onClick={onClose}
            disabled={disableClose}
            aria-label="Close"
          >
            ×
          </button>
        </div>

        <div className="modal-content">{children}</div>
      </div>
    </div>,
    document.body
  );
}
