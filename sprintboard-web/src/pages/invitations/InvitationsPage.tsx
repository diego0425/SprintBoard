import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import Header from "../../components/layout/Header";
import {
  acceptInvitation,
  declineInvitation,
} from "../../services/invitationService";
import BackButton from "../../components/common/BackButton";

export default function InvitationsPage() {
  const [searchParams] = useSearchParams();
  const hasAutoRun = useRef(false);

  const [token, setToken] = useState("");
  const [action, setAction] = useState<"accept" | "decline" | "">("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    const tokenFromQuery = searchParams.get("token");
    const actionFromQuery = searchParams.get("action");

    if (tokenFromQuery) {
      setToken(tokenFromQuery);
    }

    if (actionFromQuery === "accept" || actionFromQuery === "decline") {
      setAction(actionFromQuery);
    }
  }, [searchParams]);

  useEffect(() => {
    async function runAutoAction() {
      if (!token || !action) return;
      if (hasAutoRun.current) return;

      hasAutoRun.current = true;

      try {
        setError("");
        setSuccessMessage("");
        setIsSubmitting(true);

        if (action === "accept") {
          await acceptInvitation({ token });
          setSuccessMessage("Invitation accepted successfully.");
        }

        if (action === "decline") {
          await declineInvitation({ token });
          setSuccessMessage("Invitation declined successfully.");
        }
      } catch (autoActionError) {
        console.error(autoActionError);
        setError(
          action === "accept"
            ? "Failed to accept invitation."
            : "Failed to decline invitation."
        );
      } finally {
        setIsSubmitting(false);
      }

      setTimeout(() => {
        window.location.href = "/boards";
      }, 2000);
    }

    runAutoAction();
  }, [token, action]);

  async function handleAccept() {
    if (!token.trim()) return;

    try {
      setError("");
      setSuccessMessage("");
      setIsSubmitting(true);

      await acceptInvitation({ token: token.trim() });
      setSuccessMessage("Invitation accepted successfully.");
    } catch (acceptError) {
      console.error(acceptError);
      setError("Failed to accept invitation.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDecline() {
    if (!token.trim()) return;

    try {
      setError("");
      setSuccessMessage("");
      setIsSubmitting(true);

      await declineInvitation({ token: token.trim() });
      setSuccessMessage("Invitation declined successfully.");
    } catch (declineError) {
      console.error(declineError);
      setError("Failed to decline invitation.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <>
      <Header />

      <div className="page-back-row">
        <BackButton />
      </div>
      <div className="invitations-page">
        <div className="invitations-card">
          <h1>Board Invitation</h1>

          <p>
            {action === "accept" &&
              "We are processing your invitation acceptance."}
            {action === "decline" &&
              "We are processing your invitation decline."}
            {!action && "Paste the invitation token you received by email."}
          </p>

          {!action && (
            <div className="invitations-form">
              <input
                type="text"
                placeholder="Invitation token"
                value={token}
                onChange={(event) => setToken(event.target.value)}
              />

              <div className="invitations-actions">
                <button
                  type="button"
                  onClick={handleAccept}
                  disabled={isSubmitting}
                  className="accept-button"
                >
                  {isSubmitting ? "Processing..." : "Accept"}
                </button>

                <button
                  type="button"
                  onClick={handleDecline}
                  disabled={isSubmitting}
                  className="decline-button"
                >
                  {isSubmitting ? "Processing..." : "Decline"}
                </button>
              </div>
            </div>
          )}

          {isSubmitting && <p>Processing invitation...</p>}
          {successMessage && (
            <p className="invitation-success">{successMessage}</p>
          )}
          {error && <p className="invitation-error">{error}</p>}
        </div>
      </div>
    </>
  );
}