import {
  Compass,
  BriefcaseBusiness,
  MessageCircle,
  Bookmark as BookmarkIcon,
  Bell,
} from "lucide-react";
import type { Profile, Gig } from "../types";
export type View = "discover" | "gigs" | "inbox" | "saved" | "updates";
export type Dialog =
  | { kind: "signin" }
  | { kind: "profile"; profile: Profile }
  | { kind: "gig"; gig: Gig }
  | { kind: "profileForm"; profile?: Profile | null }
  | { kind: "post"; gig?: Gig }
  | { kind: "apply"; gig: Gig }
  | { kind: "applicants"; gig: Gig }
  | { kind: "message"; recipientId: string; name: string; gigId?: string }
  | { kind: "account" }
  | { kind: "success"; title: string; text: string }
  | {
      kind: "confirm";
      title: string;
      text: string;
      action: () => Promise<void>;
    };
export const nav = [
  { id: "discover" as View, label: "Discover", icon: Compass },
  { id: "gigs" as View, label: "Gigs", icon: BriefcaseBusiness },
  { id: "inbox" as View, label: "Inbox", icon: MessageCircle },
  { id: "saved" as View, label: "Saved", icon: BookmarkIcon },
  { id: "updates" as View, label: "Updates", icon: Bell },
];
