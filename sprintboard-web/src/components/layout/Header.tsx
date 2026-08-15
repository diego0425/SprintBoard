import { useNavigate, useLocation } from "react-router-dom";
import { KanbanSquare } from "lucide-react";
import { useAuth } from "../../hooks/useAuth";

export default function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();

  const isProfilePage = location.pathname === "/me";
  const isInvitationPage = location.pathname === "/invitations";

  function goToBoards() {
    navigate("/boards");
  }

  function getInitials() {
    if (!user?.username) return "U";
    return user.username.slice(0, 1).toUpperCase();
  }

  return (
    <header className="app-header">
      <div className="app-header-left" onClick={goToBoards}>
        <KanbanSquare size={28} />
        <span>SprintBoard</span>
      </div>

      <div className="app-header-right">
        {!isInvitationPage && (
          <button onClick={() => navigate("/invitations")}>Invitations</button>
        )}

        {!isProfilePage && (
          <button
            type="button"
            className="profile-avatar-button"
            onClick={() => navigate("/me")}
            title="Profile"
          >
            {user?.profileImageUrl ? (
              <img
                src={user.profileImageUrl}
                alt="User profile"
                className="profile-avatar-image"
              />
            ) : (
              <span className="profile-avatar-fallback">{getInitials()}</span>
            )}
          </button>
        )}
      </div>
    </header>
  );
}