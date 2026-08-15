interface AsyncStateProps {
  type: "loading" | "empty" | "error";
  title?: string;
  message: string;
  onRetry?: () => void;
  compact?: boolean;
}

export default function AsyncState({
  type,
  title,
  message,
  onRetry,
  compact = false,
}: AsyncStateProps) {
  return (
    <div className={`async-state async-state-${type}${compact ? " async-state-compact" : ""}`}>
      {title && <h2>{title}</h2>}
      <p>{message}</p>

      {type === "error" && onRetry && (
        <button type="button" className="async-state-retry" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  );
}
