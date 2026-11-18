import { Bookmark as BookmarkIcon } from "lucide-react";

import { type Bookmark } from "../types";
export function SaveButton({
  type,
  id,
  bookmarks,
  onSave,
  label,
}: {
  type: "profile" | "gig";
  id: string;
  label: string;
  bookmarks: Bookmark[];
  onSave: (type: "profile" | "gig", id: string) => void;
}) {
  const saved = bookmarks.some(
    (x) => x.targetType === type && x.targetId === id,
  );
  return (
    <button
      className={"save-button " + (saved ? "saved" : "")}
      aria-label={
        saved
          ? `Remove ${label} from saved listings`
          : `Save ${label} for later`
      }
      title={saved ? "Remove from saved listings" : "Save for later"}
      aria-pressed={saved}
      onClick={(e) => {
        e.stopPropagation();
        onSave(type, id);
      }}
    >
      <BookmarkIcon size={18} fill={saved ? "currentColor" : "none"} />
    </button>
  );
}
