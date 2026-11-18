import { ArrowUpRight, MapPin, Star } from "lucide-react";

import {
  type Profile,
  type Bookmark,
  displayName,
  money,
  categories,
  categoryIcons,
} from "../types";
import { SaveButton } from "./SaveButton";

export function TalentCard({
  profile: p,
  onOpen,
  onSave,
  bookmarks,
}: {
  profile: Profile;
  onOpen: (p: Profile) => void;
  onSave: (t: "profile" | "gig", id: string) => void;
  bookmarks: Bookmark[];
}) {
  return (
    <article className="talent-card stub">
      <div className="talent-photo">
        <button
          className="photo-link"
          onClick={() => onOpen(p)}
          aria-label={"View " + displayName(p.name)}
        >
          {p.photos[0] ? (
            <img src={p.photos[0]} alt={p.headline} loading="lazy" />
          ) : (
            <span className="category-art">
              {categoryIcons[categories.indexOf(p.category)]}
            </span>
          )}
        </button>
        <span className={"availability " + (!p.available ? "unavailable" : "")}>
          <i />
          {p.available ? "Available for gigs" : "Currently booked"}
        </span>
        <SaveButton
          type="profile"
          id={p.id}
          label={displayName(p.name)}
          bookmarks={bookmarks}
          onSave={onSave}
        />
      </div>
      <div className="talent-body">
        <div className="card-title">
          <button onClick={() => onOpen(p)}>{displayName(p.name)}</button>
          {p.name.includes("(sample)") && (
            <span className="sample">SAMPLE</span>
          )}
        </div>
        <p className="headline">{p.headline}</p>
        <div className="location">
          <MapPin size={12} />
          {p.city}
          {p.reviewCount > 0 && (
            <span className="rating">
              <Star size={12} fill="currentColor" />
              {p.rating?.toFixed(1)} ({p.reviewCount})
            </span>
          )}
        </div>
        <div className="tags">
          {p.skills.slice(0, 3).map((x) => (
            <span key={x}>{x}</span>
          ))}
        </div>
        <div className="card-bottom">
          <strong>
            {p.rate ? money(p.rate) : "Let’s discuss"}{" "}
            <small>{p.rate ? p.rateUnit : ""}</small>
          </strong>
          <button onClick={() => onOpen(p)}>
            View profile <ArrowUpRight size={14} />
          </button>
        </div>
      </div>
    </article>
  );
}
