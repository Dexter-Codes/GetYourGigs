import { useState } from "react";
import {
  ArrowUpRight,
  Bell,
  CheckCheck,
  MessageCircle,
  Sparkles,
} from "lucide-react";
import { api } from "../api";

import { useResource } from "../hooks/useResource";
import { Empty, ErrorBox, Loading } from "../components";
import { type Gig, type Activity } from "../types";

export function Updates({
  revision,
  onGig,
}: {
  revision: number;
  onGig: (g: Gig) => void;
}) {
  const [tick, setTick] = useState(0),
    [error, setError] = useState("");
  const updates = useResource<Activity[]>("/me/updates", revision + tick);
  return (
    <section className="page">
      <div className="section-head">
        <div>
          <span className="eyebrow">WHILE YOU WERE MAKING THINGS HAPPEN</span>
          <h1>You’re all caught up. Almost.</h1>
          <p>
            Bookings, replies, and little steps forward. Your latest updates
            live here.
          </p>
        </div>
        <button
          className="btn outline small"
          onClick={async () => {
            try {
              await api("/me/updates/read", "POST");
              setTick((x) => x + 1);
            } catch (e) {
              setError((e as Error).message);
            }
          }}
        >
          <CheckCheck size={15} />
          Mark all read
        </button>
      </div>
      <ErrorBox message={updates.error || error} />
      {updates.loading ? (
        <Loading />
      ) : !updates.data?.length ? (
        <Empty title="Quiet for now. Possibilities ahead.">
          New applications, messages, bookings, and reviews will appear here.
        </Empty>
      ) : (
        <div className="updates-list">
          {updates.data.map((x) => (
            <article
              key={x.id}
              className={"update-item " + (!x.read ? "unread" : "")}
            >
              <span className="update-icon">
                {x.kind === "message" ? (
                  <MessageCircle size={20} />
                ) : x.kind === "application" ? (
                  <Sparkles size={20} />
                ) : (
                  <Bell size={20} />
                )}
              </span>
              <div>
                <p>{x.text}</p>
                <time>{new Date(x.createdAt).toLocaleString("en-IN")}</time>
              </div>
              {x.gigId && (
                <button
                  className="icon-button"
                  aria-label="View gig"
                  onClick={async () => {
                    try {
                      onGig(await api<Gig>("/gigs/" + x.gigId));
                    } catch (e) {
                      setError((e as Error).message);
                    }
                  }}
                >
                  <ArrowUpRight size={18} />
                </button>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
