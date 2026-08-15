import { useCallback, useEffect, useState } from "react";
import { LogOut, ShieldCheck, Trash2, UserRound } from "lucide-react";
import {
  changeBoardMemberRole,
  getBoardMembers,
  leaveBoard,
  removeBoardMember,
} from "../../services/boardService";
import { BoardRole, type BoardMember } from "../../types/board";
import SideDrawer from "../ui/SideDrawer";
import AsyncState from "../ui/AsyncState";
import ConfirmDialog from "../ui/ConfirmDialog";

interface MembersDrawerProps {
  boardId: string;
  currentUserId?: string;
  currentUserRole: BoardRole | null;
  isOpen: boolean;
  onClose: () => void;
  onBoardLeft: () => void;
}

const roleLabels: Record<BoardRole, string> = {
  [BoardRole.Owner]: "Owner",
  [BoardRole.Admin]: "Admin",
  [BoardRole.Member]: "Member",
};

export default function MembersDrawer({
  boardId,
  currentUserId,
  currentUserRole,
  isOpen,
  onClose,
  onBoardLeft,
}: MembersDrawerProps) {
  const [members, setMembers] = useState<BoardMember[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [updatingMemberId, setUpdatingMemberId] = useState<string | null>(null);
  const [memberPendingRemoval, setMemberPendingRemoval] = useState<BoardMember | null>(null);
  const [isRemovingMember, setIsRemovingMember] = useState(false);
  const [isLeaveDialogOpen, setIsLeaveDialogOpen] = useState(false);
  const [isLeavingBoard, setIsLeavingBoard] = useState(false);
  const [error, setError] = useState("");

  const canEditRoles = currentUserRole === BoardRole.Owner;
  const canLeaveBoard =
    currentUserRole === BoardRole.Admin || currentUserRole === BoardRole.Member;
  const isBusy =
    isLoading ||
    Boolean(updatingMemberId) ||
    isRemovingMember ||
    isLeavingBoard;

  const loadMembers = useCallback(async () => {
    try {
      setError("");
      setIsLoading(true);
      const boardMembers = await getBoardMembers(boardId);
      setMembers(boardMembers);
    } catch (loadError) {
      console.error(loadError);
      setError("Failed to load board members.");
    } finally {
      setIsLoading(false);
    }
  }, [boardId]);

  useEffect(() => {
    if (isOpen) {
      loadMembers();
    }
  }, [isOpen, loadMembers]);

  async function handleRoleChange(member: BoardMember, newRole: BoardRole) {
    if (
      isBusy ||
      !canEditRoles ||
      member.role === newRole ||
      member.role === BoardRole.Owner ||
      newRole === BoardRole.Owner
    ) {
      return;
    }

    const previousRole = member.role;

    try {
      setError("");
      setUpdatingMemberId(member.userId);
      setMembers((currentMembers) =>
        currentMembers.map((currentMemberItem) =>
          currentMemberItem.userId === member.userId
            ? { ...currentMemberItem, role: newRole }
            : currentMemberItem
        )
      );

      await changeBoardMemberRole(boardId, {
        memberUserId: member.userId,
        newRole,
      });
    } catch (updateError) {
      console.error(updateError);
      setMembers((currentMembers) =>
        currentMembers.map((currentMemberItem) =>
          currentMemberItem.userId === member.userId
            ? { ...currentMemberItem, role: previousRole }
            : currentMemberItem
        )
      );
      setError("Failed to update member role.");
    } finally {
      setUpdatingMemberId(null);
    }
  }

  function canRemoveMember(member: BoardMember) {
    if (member.userId === currentUserId || member.role === BoardRole.Owner) {
      return false;
    }

    if (currentUserRole === BoardRole.Owner) {
      return true;
    }

    return (
      currentUserRole === BoardRole.Admin &&
      member.role === BoardRole.Member
    );
  }

  async function handleRemoveMember() {
    if (!memberPendingRemoval || isRemovingMember) return;

    try {
      setError("");
      setIsRemovingMember(true);

      await removeBoardMember(boardId, memberPendingRemoval.userId);

      setMembers((currentMembers) =>
        currentMembers.filter(
          (member) => member.userId !== memberPendingRemoval.userId
        )
      );
      setMemberPendingRemoval(null);
    } catch (removeError) {
      console.error(removeError);
      setError("Failed to remove member from the board.");
      setMemberPendingRemoval(null);
    } finally {
      setIsRemovingMember(false);
    }
  }

  async function handleLeaveBoard() {
    if (!canLeaveBoard || isLeavingBoard) return;

    try {
      setError("");
      setIsLeavingBoard(true);
      await leaveBoard(boardId);
      setIsLeaveDialogOpen(false);
      onBoardLeft();
    } catch (leaveError) {
      console.error(leaveError);
      setError("Failed to leave the board.");
      setIsLeaveDialogOpen(false);
    } finally {
      setIsLeavingBoard(false);
    }
  }

  function getPermissionHint() {
    if (currentUserRole === BoardRole.Owner) {
      return "As owner, you can change roles and remove admins or members.";
    }

    if (currentUserRole === BoardRole.Admin) {
      return "Admins can invite people and remove members. Only the owner can change roles.";
    }

    return "Only the board owner can change roles or remove other members.";
  }

  return (
    <>
      <SideDrawer
        title="Board members"
        isOpen={isOpen}
        onClose={onClose}
        disableClose={isRemovingMember || isLeavingBoard}
      >
        <div className="members-drawer-toolbar">
          <p>{members.length} member{members.length === 1 ? "" : "s"}</p>
          <button
            type="button"
            onClick={loadMembers}
            disabled={isBusy}
          >
            Refresh
          </button>
        </div>

        {!isLoading && members.length > 0 && (
          <p className="members-drawer-hint">{getPermissionHint()}</p>
        )}

        {isLoading ? (
          <AsyncState type="loading" message="Loading members..." compact />
        ) : error && members.length === 0 ? (
          <AsyncState type="error" message={error} onRetry={loadMembers} compact />
        ) : members.length === 0 ? (
          <AsyncState type="empty" message="No members found." compact />
        ) : (
          <>
            {error && <p className="members-drawer-error">{error}</p>}
            <ul className="members-list">
              {members.map((member) => {
                const isOwner = member.role === BoardRole.Owner;
                const isUpdating = updatingMemberId === member.userId;
                const showRemoveButton = canRemoveMember(member);

                return (
                  <li key={member.userId} className="member-list-item">
                    <div className="member-identity">
                      <div className="member-avatar">
                        {member.profileImageUrl ? (
                          <img
                            src={member.profileImageUrl}
                            alt={`${member.username}'s profile`}
                            className="member-avatar-image"
                          />
                        ) : isOwner ? (
                          <ShieldCheck size={18} />
                        ) : (
                          <UserRound size={18} />
                        )}
                      </div>

                      <div className="member-details">
                        <strong>
                          {member.username}
                          {member.userId === currentUserId ? " (you)" : ""}
                        </strong>
                        <span>{isUpdating ? "Updating..." : roleLabels[member.role]}</span>
                      </div>
                    </div>

                    <div className="member-actions">
                      <select
                        className="member-role-select"
                        value={member.role}
                        disabled={isOwner || !canEditRoles || isBusy}
                        onChange={(event) =>
                          handleRoleChange(
                            member,
                            Number(event.target.value) as BoardRole
                          )
                        }
                        aria-label={`Role for ${member.username}`}
                      >
                        {isOwner && <option value={BoardRole.Owner}>Owner</option>}
                        {!isOwner && (
                          <>
                            <option value={BoardRole.Admin}>Admin</option>
                            <option value={BoardRole.Member}>Member</option>
                          </>
                        )}
                      </select>

                      {showRemoveButton && (
                        <button
                          type="button"
                          className="member-remove-button"
                          onClick={() => setMemberPendingRemoval(member)}
                          disabled={isBusy}
                          title={`Remove ${member.username}`}
                          aria-label={`Remove ${member.username} from board`}
                        >
                          <Trash2 size={17} />
                        </button>
                      )}
                    </div>
                  </li>
                );
              })}
            </ul>

            {canLeaveBoard && (
              <div className="members-drawer-footer">
                <button
                  type="button"
                  className="leave-board-button"
                  onClick={() => setIsLeaveDialogOpen(true)}
                  disabled={isBusy}
                >
                  <LogOut size={17} />
                  Leave board
                </button>
              </div>
            )}
          </>
        )}
      </SideDrawer>

      <ConfirmDialog
        isOpen={Boolean(memberPendingRemoval)}
        title="Remove member"
        message={
          memberPendingRemoval
            ? `Remove ${memberPendingRemoval.username} from this board? They will lose access to its content.`
            : "Remove this member from the board?"
        }
        confirmText="Remove member"
        loadingText="Removing..."
        onConfirm={handleRemoveMember}
        onCancel={() => setMemberPendingRemoval(null)}
        isLoading={isRemovingMember}
      />

      <ConfirmDialog
        isOpen={isLeaveDialogOpen}
        title="Leave board"
        message="Are you sure you want to leave this board? You will lose access until you are invited again."
        confirmText="Leave board"
        loadingText="Leaving..."
        onConfirm={handleLeaveBoard}
        onCancel={() => setIsLeaveDialogOpen(false)}
        isLoading={isLeavingBoard}
      />
    </>
  );
}
