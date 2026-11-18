import { useEffect, useState } from "react";

import { api } from "../api";

import { Empty, ErrorBox, GigCard, Loading, TalentCard } from "../components";
import { type Mode, type Profile, type Gig } from "../types";
import type { ListingActions } from "./ListingActions";

export function Saved({
  mode,
  bookmarks,
  revision,
  ...actions
}: { mode: Mode; revision: number } & ListingActions) {
  const type = mode === "Hiring" ? "profile" : "gig";
  const [items, setItems] = useState<(Profile | Gig)[]>([]),
    [loading, setLoading] = useState(true),
    [error, setError] = useState("");
  const [loadedType, setLoadedType] = useState(type);
  useEffect(() => {
    let active = true;
    const controller = new AbortController();
    setItems([]);
    setLoading(true);
    setError("");
    Promise.all(
      bookmarks
        .filter((x) => x.targetType === type)
        .map((x) =>
          api<Profile | Gig>(
            "/" + (type === "profile" ? "profiles" : "gigs") + "/" + x.targetId,
            "GET",
            undefined,
            controller.signal,
          ),
        ),
    )
      .then((x) => {
        if (active) setItems(x);
      })
      .catch((e) => {
        if (active) setError(e.message);
      })
      .finally(() => {
        if (active) {
          setLoading(false);
          setLoadedType(type);
        }
      });
    return () => {
      active = false;
      controller.abort();
    };
  }, [type, bookmarks, revision]);
  return (
    <section className="page">
      <div className="section-head">
        <div>
          <span className="eyebrow">THE ONES YOU WANT TO COME BACK TO</span>
          <h1>A little collection of possibility.</h1>
          <p>
            {mode === "Hiring"
              ? "Talent you’ve saved for your next big thing."
              : "Gigs that caught your eye. Make your next move."}
          </p>
        </div>
        <span className="count-label">
          {items.length} saved {type === "profile" ? "people" : "gigs"}
        </span>
      </div>
      <ErrorBox message={error} />
      {loading || loadedType !== type ? (
        <Loading />
      ) : items.length ? (
        <div className={type === "profile" ? "talent-grid" : "gig-grid"}>
          {items.map((x) =>
            type === "profile" ? (
              <TalentCard
                key={x.id}
                profile={x as Profile}
                onOpen={actions.onProfile}
                onSave={actions.onSave}
                bookmarks={bookmarks}
              />
            ) : (
              <GigCard
                key={x.id}
                gig={x as Gig}
                onOpen={actions.onGig}
                onSave={actions.onSave}
                bookmarks={bookmarks}
              />
            ),
          )}
        </div>
      ) : (
        <Empty title="Keep a little possibility for later.">
          Tap the bookmark on a {type === "profile" ? "talent profile" : "gig"}{" "}
          and it will be waiting here.
        </Empty>
      )}
    </section>
  );
}
