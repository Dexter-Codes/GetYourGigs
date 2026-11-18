import { useEffect, useState } from "react";
import { CalendarDays, Plus } from "lucide-react";
import { api } from "../api";

import { useResource } from "../hooks/useResource";
import { Empty, ErrorBox, GigCard, Loading, Status } from "../components";
import {
  type Account,
  type Mode,
  type Gig,
  type Application,
  displayName,
  dateLabel,
} from "../types";
import type { ListingActions } from "./ListingActions";

export function MyGigs({
  user,
  mode,
  revision,
  onPost,
  onBrowse,
  onWithdraw,
  ...actions
}: {
  user: Account;
  mode: Mode;
  revision: number;
  onPost: () => void;
  onBrowse: () => void;
  onWithdraw: (id: string) => void;
} & ListingActions) {
  const hiring = mode === "Hiring";
  const [filter, setFilter] = useState("All");
  const { data, loading, error } = useResource<Gig[] | Application[]>(
    hiring ? "/me/gigs" : "/me/applications",
    revision,
  );
  useEffect(() => setFilter("All"), [mode]);
  return (
    <section className="page">
      <div className="section-head">
        <div>
          <span className="eyebrow">YOUR WORK, ALL IN ONE PLACE</span>
          <h1>
            {hiring
              ? "Your gigs. Your good people."
              : "The next steps in your story."}
          </h1>
          <p>
            {hiring
              ? "Manage your briefs, meet applicants, and make things happen."
              : "Every application, every update, every new beginning."}
          </p>
        </div>
        <button className="btn primary" onClick={hiring ? onPost : onBrowse}>
          <Plus size={17} />
          {hiring ? "Post a gig" : "Find a gig"}
        </button>
      </div>
      <div className="chips">
        {(hiring
          ? ["All", "Open", "Booked", "Completed", "Closed"]
          : [
              "All",
              "Applied",
              "Shortlisted",
              "Booked",
              "Waitlisted",
              "Completed",
              "Cancelled",
            ]
        ).map((x) => (
          <button
            className={filter === x ? "active" : ""}
            key={x}
            onClick={() => setFilter(x)}
          >
            {x}
          </button>
        ))}
      </div>
      <ErrorBox message={error} />
      {loading ? (
        <Loading />
      ) : !data?.length ? (
        <Empty
          title={
            hiring
              ? "Your next great project starts here."
              : "Take a chance on something good."
          }
          action={
            <button
              className="btn primary"
              onClick={hiring ? onPost : onBrowse}
            >
              {hiring ? "Post your first gig" : "Explore open gigs"}
            </button>
          }
        >
          {hiring
            ? "Post a brief and let the right people find you."
            : "Apply to a gig and track your progress right here."}
        </Empty>
      ) : hiring ? (
        <div className="gig-grid">
          {(data as Gig[])
            .filter((x) => filter === "All" || x.status === filter)
            .map((g) => (
              <GigCard
                key={g.id}
                gig={g}
                onOpen={actions.onGig}
                onSave={actions.onSave}
                bookmarks={actions.bookmarks}
                own={g.ownerId === user.id}
              />
            ))}
        </div>
      ) : (
        <div className="application-list">
          {(data as Application[])
            .filter((x) => filter === "All" || x.status === filter)
            .map((a) => (
              <article className="application-card" key={a.id}>
                <div className="application-main">
                  <span className="application-icon">
                    <CalendarDays size={23} />
                  </span>
                  <div>
                    <button
                      className="application-title"
                      onClick={async () =>
                        actions.onGig(await api<Gig>("/gigs/" + a.gigId))
                      }
                    >
                      {displayName(a.gigTitle)}
                    </button>
                    <p>
                      {dateLabel(a.date)} · Applied{" "}
                      {new Date(a.createdAt).toLocaleDateString("en-IN")}
                    </p>
                  </div>
                  <Status value={a.status} />
                </div>
                <p className="application-note">{a.message}</p>
                {["Applied", "Shortlisted", "Waitlisted", "Booked"].includes(
                  a.status,
                ) && (
                  <button
                    className="link danger"
                    onClick={() => onWithdraw(a.id)}
                  >
                    Withdraw {a.status === "Booked" ? "booking" : "application"}
                  </button>
                )}
              </article>
            ))}
        </div>
      )}
    </section>
  );
}
