import { ArrowRight, MapPin, Star } from "lucide-react";

import { useResource } from "../hooks/useResource";
import { ErrorBox, Loading } from "../components";
import { type Profile, type Review, displayName, money } from "../types";

export function TalentDetail({
  profile: p,
  onMessage,
  onSaved,
  saved,
}: {
  profile: Profile;
  saved: boolean;
  onMessage: () => void;
  onSaved: () => void;
}) {
  const reviews = useResource<Review[]>("/profiles/" + p.id + "/reviews");
  return (
    <>
      {p.photos[0] && (
        <img className="detail-cover" src={p.photos[0]} alt={p.headline} />
      )}
      <div className="detail-heading">
        <div>
          <span className="eyebrow">{p.category}</span>
          <h2>{displayName(p.name)}</h2>
          <p>{p.headline}</p>
        </div>
        <span className="availability inline">
          <i />
          {p.available ? "Available" : "Currently booked"}
        </span>
      </div>
      <div className="detail-meta">
        <span>
          <MapPin size={15} />
          {p.city}
        </span>
        <strong>
          {p.rate ? money(p.rate) + " " + p.rateUnit : "Let’s discuss rates"}
        </strong>
      </div>
      <div className="detail-section">
        <h4>About me</h4>
        <p className="preserve-whitespace">{p.bio}</p>
      </div>
      <div className="tags">
        {p.skills.map((s, i) => (
          <span key={i}>{s}</span>
        ))}
      </div>
      {p.photos.length > 1 && (
        <div className="portfolio">
          {p.photos.slice(1).map((x) => (
            <img key={x} src={x} alt="Portfolio work" />
          ))}
        </div>
      )}
      <div className="detail-actions">
        <button className="btn outline" onClick={onSaved}>
          {saved ? "Saved ✓ · remove" : "Save for later"}
        </button>
        <button className="btn primary" onClick={onMessage}>
          Message / Hire <ArrowRight size={16} />
        </button>
      </div>
      <div className="detail-section">
        <h4>Reviews from completed gigs</h4>
        {reviews.loading ? (
          <Loading />
        ) : reviews.error ? (
          <ErrorBox message={reviews.error} />
        ) : reviews.data?.length ? (
          reviews.data.map((r) => (
            <div className="review" key={r.id}>
              <strong>{displayName(r.hirerName)}</strong>
              <span className="rating">
                <Star size={13} fill="currentColor" />
                {r.rating}/5
              </span>
              <p>{r.text}</p>
              <small>{displayName(r.gigTitle)}</small>
            </div>
          ))
        ) : (
          <p>No completed-gig reviews yet. Good work will speak for itself.</p>
        )}
      </div>
    </>
  );
}
