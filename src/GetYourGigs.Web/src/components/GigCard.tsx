import { MapPin, CalendarDays, ChevronRight } from "lucide-react";

import {
  type Gig,
  type Bookmark,
  displayName,
  dateLabel,
  money,
  categories,
  categoryIcons,
} from "../types";
import { Status } from "./Status";
import { SaveButton } from "./SaveButton";

export function GigCard({
  gig: g,
  onOpen,
  onSave,
  bookmarks,
  own = false,
}: {
  gig: Gig;
  onOpen: (g: Gig) => void;
  onSave: (t: "profile" | "gig", id: string) => void;
  bookmarks: Bookmark[];
  own?: boolean;
}) {
  return (
    <article className="gig-card stub">
      <div className="gig-top">
        <span
          className={"category-bubble tile-" + categories.indexOf(g.category)}
        >
          {categoryIcons[categories.indexOf(g.category)]}
        </span>
        <div>
          <span className="mini-label">{g.category}</span>
          <small>{own ? "Your posted gig" : displayName(g.ownerName)}</small>
        </div>
        <SaveButton
          type="gig"
          id={g.id}
          label={displayName(g.title)}
          bookmarks={bookmarks}
          onSave={onSave}
        />
      </div>
      <button className="gig-title" onClick={() => onOpen(g)}>
        {displayName(g.title)}
      </button>
      <p className="gig-description">{g.description}</p>
      <div className="gig-meta">
        <span>
          <MapPin size={13} />
          {g.city}
        </span>
        <span>
          <CalendarDays size={13} />
          {dateLabel(g.date)}
        </span>
      </div>
      <div className="card-bottom">
        <strong>
          {g.budget ? money(g.budget) : "Open budget"}{" "}
          <small>project budget</small>
        </strong>
        {own ? (
          <Status value={g.status} />
        ) : (
          <button onClick={() => onOpen(g)}>
            View gig <ChevronRight size={14} />
          </button>
        )}
      </div>
      {g.title.includes("(sample)") && (
        <div className="sample-note">Sample listing · explore the workflow</div>
      )}
    </article>
  );
}
