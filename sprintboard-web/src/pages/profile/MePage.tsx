import { useState } from "react";
import Header from "../../components/layout/Header";
import BackButton from "../../components/common/BackButton";
import { useAuth } from "../../hooks/useAuth";
import { getMe, updateMe, updateProfileImage } from "../../services/userService";

export default function MePage() {
  const { user, setUser, logout } = useAuth();

  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [username, setUsername] = useState(user?.username ?? "");
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [error, setError] = useState("");

  function getInitials() {
    if (!user?.username) {
      return "U";
    }

    return user.username.slice(0, 1).toUpperCase();
  }

  async function refreshCurrentUser() {
    const currentUser = await getMe();

    setUser({
      userId: currentUser.id,
      fullName: currentUser.fullName,
      username: currentUser.username,
      email: currentUser.email,
      profileImageUrl: currentUser.profileImageUrl,
    });
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      setError("");
      setSuccessMessage("");
      setIsSubmitting(true);

      await updateMe({
        fullName: fullName.trim() || undefined,
        username: username.trim() || undefined,
        oldPassword: oldPassword.trim() || undefined,
        newPassword: newPassword.trim() || undefined,
      });

      await refreshCurrentUser();
      setOldPassword("");
      setNewPassword("");
      setSuccessMessage("Profile updated successfully.");
    } catch (updateError) {
      console.error(updateError);
      setError("Failed to update profile.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleImageUpload() {
    if (!selectedFile) {
      return;
    }

    try {
      setError("");
      setSuccessMessage("");
      setIsUploadingImage(true);

      await updateProfileImage(selectedFile);
      await refreshCurrentUser();

      setSelectedFile(null);
      setSuccessMessage("Profile image updated successfully.");
    } catch (uploadError) {
      console.error(uploadError);
      setError("Failed to update profile image.");
    } finally {
      setIsUploadingImage(false);
    }
  }

  return (
    <>
      <Header />

      <div className="page-back-row">
        <BackButton />
      </div>

      <main className="profile-page">
        <section className="profile-card">
          <h1>My profile</h1>

          <div className="profile-image-section">
            <div className="profile-image-preview">
              {user?.profileImageUrl ? (
                <img
                  src={user.profileImageUrl}
                  alt="User profile"
                  className="profile-page-avatar-image"
                />
              ) : (
                <div className="profile-page-avatar-fallback">{getInitials()}</div>
              )}
            </div>

            <div className="profile-image-actions">
              <input
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(event) =>
                  setSelectedFile(event.target.files?.[0] ?? null)
                }
              />

              <button
                type="button"
                className="success-button"
                onClick={handleImageUpload}
                disabled={!selectedFile || isUploadingImage}
              >
                {isUploadingImage ? "Uploading..." : "Save image"}
              </button>
            </div>
          </div>

          <form className="profile-form" onSubmit={handleSubmit}>
            <input
              type="text"
              placeholder="Full name"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
            />

            <input
              type="text"
              placeholder="Username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
            />

            <input
              type="password"
              placeholder="Old password"
              value={oldPassword}
              onChange={(event) => setOldPassword(event.target.value)}
            />

            <input
              type="password"
              placeholder="New password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
            />

            <div className="profile-actions">
              <button type="button" className="logout-button" onClick={logout}>
                Logout
              </button>

              <button
                type="submit"
                className="success-button"
                disabled={isSubmitting}
              >
                {isSubmitting ? "Saving..." : "Save changes"}
              </button>
            </div>
          </form>

          {successMessage && <p className="status-success">{successMessage}</p>}
          {error && <p className="status-error">{error}</p>}
        </section>
      </main>
    </>
  );
}
