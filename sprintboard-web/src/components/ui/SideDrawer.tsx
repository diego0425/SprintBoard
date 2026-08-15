import { useEffect, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";

interface SideDrawerProps {
  title: string;
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
  disableClose?: boolean;
}

export default function SideDrawer({
  title,
  isOpen,
  onClose,
  children,
  disableClose = false,
}: SideDrawerProps) {
  useEffect(() => {
    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape" && !disableClose) {
        onClose();
      }
    }

    if (isOpen) {
      window.addEventListener("keydown", handleEscape);
      document.body.style.overflow = "hidden";
    }

    return () => {
      window.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "";
    };
  }, [isOpen, onClose, disableClose]);

  if (!isOpen) {
    return null;
  }

  return createPortal(
    <div
      className="drawer-overlay"
      onClick={() => {
        if (!disableClose) onClose();
      }}
    >
      <aside
        className="side-drawer"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(event) => event.stopPropagation()}
      >
        <header className="side-drawer-header">
          <h2>{title}</h2>
          <button
            type="button"
            className="side-drawer-close-button"
            onClick={onClose}
            aria-label="Close"
            disabled={disableClose}
          >
            <X size={20} />
          </button>
        </header>

        <div className="side-drawer-content">{children}</div>
      </aside>
    </div>,
    document.body
  );
}
