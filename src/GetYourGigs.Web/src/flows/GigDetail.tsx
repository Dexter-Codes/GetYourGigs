import { ArrowRight } from "lucide-react";

import { Avatar, Status } from "../components";
import { type Gig, displayName, dateLabel, money } from "../types";

export function GigDetail({
  gig: g,
  own,
  onApply,
  onMessage,
  onApplicants,
  onEdit,
  onComplete,
  onCloseGig,
}: {
  gig: Gig;
  own: boolean;
  onApply: () => void;
  onMessage: () => void;
  onApplicants: () => void;
  onEdit: () => void;
  onComplete: () => void;
  onCloseGig: () => void;
}) {
  return (
    <>
      {g.photos[0] && (
        <img
          className="detail-cover"
          src={g.photos[0]}
          alt={displayName(g.title)}
        />
      )}
      <span className="eyebrow">{g.category}</span>
      <h2>{displayName(g.title)}</h2>
      <div className="detail-meta">
        <Avatar name={g.ownerName} />
        <span>Posted by {displayName(g.ownerName)}</span>
        <Status value={g.status} />
      </div>
      <div className="detail-facts">
        <div>
          <small>Location</small>
          <strong>{g.city}</strong>
        </div>
        <div>
          <small>Date</small>
          <strong>{dateLabel(g.date)}</strong>
        </div>
        <div>
          <small>Budget</small>
          <strong>{g.budget ? money(g.budget) : "Let’s discuss"}</strong>
        </div>
      </div>
      <div className="detail-section">
        <h4>The brief</h4>
        <p className="preserve-whitespace">{g.description}</p>
      </div>
      {own ? (
        <>
          <div className="detail-actions">
            <button className="btn outline" onClick={onApplicants}>
              {g.applicationCount} applicants
            </button>
            {g.status === "Open" && (
              <button className="btn primary" onClick={onEdit}>
                Edit gig
              </button>
            )}
            {g.status === "Booked" && (
              <button className="btn primary" onClick={onComplete}>
                Confirm completed
              </button>
            )}
          </div>
          {g.status === "Open" && (
            <button className="link danger centered" onClick={onCloseGig}>
              Close this gig
            </button>
          )}
        </>
      ) : (
        <div className="detail-actions">
          <button className="btn outline" onClick={onMessage}>
            Message hirer
          </button>
          <button
            className="btn primary"
            onClick={onApply}
            disabled={g.status !== "Open"}
          >
            {g.status === "Open" ? "Apply to this gig" : "Applications closed"}{" "}
            <ArrowRight size={16} />
          </button>
        </div>
      )}
    </>
  );
}
