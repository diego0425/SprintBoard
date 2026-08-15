import { useState } from "react";
import { inviteMemberToBoard } from "../../services/boardService";

interface InviteMemberFormProps {
  boardId: string;
  onSuccess?: () => void;
}

export default function InviteMemberForm({ boardId, onSuccess }: InviteMemberFormProps) {
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [error, setError] = useState("");

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!email.trim()) return;

    try {
      setError("");
      setSuccessMessage("");
      setIsSubmitting(true);

      await inviteMemberToBoard(boardId, { email: email.trim() });

      setSuccessMessage("Invitation sent successfully.");
      setEmail("");
      
      if (onSuccess) {
        onSuccess();
      }
      
    } catch (inviteError) {
      console.error(inviteError);
      setError("Failed to send invitation.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="invite-member-panel">
      <h2>Invite member</h2>

      <form className="invite-member-form" onSubmit={handleSubmit}>
        <input
          type="email"
          placeholder="member@email.com"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Sending..." : "Send invite"}
        </button>
      </form>

      {successMessage && <p className="invite-success">{successMessage}</p>}
      {error && <p className="invite-error">{error}</p>}
    </section>
  );
}