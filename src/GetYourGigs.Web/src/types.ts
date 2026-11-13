export type Mode = "Hiring" | "Working";
export type GigStatus = "Open" | "Booked" | "Completed" | "Closed";
export type AppStatus =
  | "Applied"
  | "Shortlisted"
  | "Booked"
  | "Waitlisted"
  | "Declined"
  | "Cancelled"
  | "Completed"
  | "NotSelected";
export interface Account {
  id: string;
  name: string;
  email: string;
  preferredMode: Mode;
  hasProfile: boolean;
}
export interface Auth {
  accessToken: string;
  expiresAt: string;
  account: Account;
}
export interface Profile {
  id: string;
  ownerId: string;
  name: string;
  headline: string;
  category: string;
  city: string;
  rate: number;
  rateUnit: string;
  bio: string;
  skills: string[];
  photos: string[];
  available: boolean;
  rating: number | null;
  reviewCount: number;
}
export interface Gig {
  id: string;
  ownerId: string;
  ownerName: string;
  title: string;
  category: string;
  city: string;
  description: string;
  date: string | null;
  budget: number;
  status: GigStatus;
  photos: string[];
  applicationCount: number;
  createdAt: string;
}
export interface Application {
  id: string;
  gigId: string;
  gigTitle: string;
  date: string | null;
  applicantId: string;
  applicantName: string;
  profileId: string | null;
  message: string;
  expectedPrice: number | null;
  status: AppStatus;
  createdAt: string;
}
export interface Bookmark {
  id: string;
  targetType: "profile" | "gig";
  targetId: string;
}
export interface Thread {
  id: string;
  otherUserId: string;
  otherName: string;
  gigId: string | null;
  gigTitle: string | null;
  lastMessage: string;
  updatedAt: string;
  unreadCount: number;
}
export interface Message {
  id: string;
  senderId: string;
  senderName: string;
  text: string;
  createdAt: string;
}
export interface Activity {
  id: string;
  kind: string;
  text: string;
  gigId: string | null;
  createdAt: string;
  read: boolean;
}
export interface Review {
  id: string;
  hirerName: string;
  gigTitle: string;
  rating: number;
  text: string;
  createdAt: string;
}
export interface Page<T> {
  items: T[];
  total: number;
  isPreview: boolean;
  page: number;
  pageSize: number;
}
export interface Config {
  googleClientId: string | null;
  developmentEmail: boolean;
  categories: string[];
}
export interface ProfileInput {
  headline: string;
  category: string;
  city: string;
  rate: number;
  bio: string;
  skills: string[];
  photos: string[];
  rateUnit: string;
  available: boolean;
}
export interface GigInput {
  title: string;
  category: string;
  city: string;
  description: string;
  date: string | null;
  budget: number;
  photos: string[];
}
export const categories = [
  "Music & Performance",
  "Home & Trade Services",
  "Creative & Design",
  "Tech & Development",
  "Events & Hospitality",
  "Coaching & Wellness",
  "Business & Admin",
  "Other",
];
export const categoryIcons = ["♫", "⌁", "✹", "⌘", "♜", "❋", "▣", "•••"];
export const money = (n: number) =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(n);
export const dateLabel = (s: string | null) =>
  s
    ? new Date(s + "T12:00:00").toLocaleDateString("en-IN", {
        day: "numeric",
        month: "short",
        year: "numeric",
      })
    : "Date to be agreed";
export const displayName = (s: string) => s.replace(" (sample)", "");
export const initials = (s: string) =>
  displayName(s)
    .split(" ")
    .slice(0, 2)
    .map((x) => x[0])
    .join("");
