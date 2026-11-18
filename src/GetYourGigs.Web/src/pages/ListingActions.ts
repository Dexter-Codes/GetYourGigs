import type { Profile, Gig, Bookmark } from "../types";

export interface ListingActions {
  onProfile: (p: Profile) => void;
  onGig: (g: Gig) => void;
  onSave: (type: "profile" | "gig", id: string) => void;
  bookmarks: Bookmark[];
}
