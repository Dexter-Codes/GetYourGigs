import { useState } from "react";

import { api } from "../api";
import { useResource } from "../hooks/useResource";
import {
  ActionForm,
  Avatar,
  Empty,
  ErrorBox,
  Field,
  Loading,
  Status,
} from "../components";
import { type Gig, type Application, displayName, money } from "../types";

export function Applicants({
  gig,
  onChanged,
  onProfile,
  onMessage,
}: {
  gig: Gig;
  onChanged: () => void;
  onProfile: (id: string) => void;
  onMessage: (a: Application) => void;
}) {
  const [version, setVersion] = useState(0),
    [busy, setBusy] = useState(""),
    [error, setError] = useState(""),
    [filter, setFilter] = useState("All");
  const {
    data,
    loading,
    error: loadError,
  } = useResource<Application[]>("/gigs/" + gig.id + "/applications", version);
  async function change(id: string, status: string) {
    setBusy(id);
    setError("");
    try {
      await api("/applications/" + id + "/status", "PUT", { status });
      setVersion((x) => x + 1);
      onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy("");
    }
  }
  return (
    <>
      <span className="eyebrow">THE RIGHT PEOPLE, RIGHT HERE</span>
      <h2>Meet your applicants.</h2>
      <p>{displayName(gig.title)}</p>
      <div className="chips">
        {[
          "All",
          "Applied",
          "Shortlisted",
          "Booked",
          "Waitlisted",
          "Completed",
        ].map((x) => (
          <button
            key={x}
            className={filter === x ? "active" : ""}
            onClick={() => setFilter(x)}
          >
            {x}
          </button>
        ))}
      </div>
      <ErrorBox message={error || loadError} />
      {loading ? (
        <Loading />
      ) : !data?.length ? (
        <Empty title="Good things take a little time.">
          Applications to this gig will appear here.
        </Empty>
      ) : (
        data
          .filter((x) => filter === "All" || x.status === filter)
          .map((a) => (
            <div key={a.id} className="applicant">
              <div className="applicant-heading">
                <Avatar name={a.applicantName} />
                <div>
                  <button
                    className="link strong"
                    onClick={() => a.profileId && onProfile(a.profileId)}
                  >
                    {displayName(a.applicantName)}
                  </button>
                  <small>
                    {a.expectedPrice
                      ? money(a.expectedPrice)
                      : "Price to be discussed"}
                  </small>
                </div>
                <Status value={a.status} />
              </div>
              <p className="preserve-whitespace">{a.message}</p>
              <div className="applicant-actions">
                <button
                  className="btn small outline"
                  onClick={() => onMessage(a)}
                >
                  Message
                </button>
                {["Applied", "Shortlisted", "Waitlisted"].includes(
                  a.status,
                ) && (
                  <>
                    <button
                      className="btn small outline"
                      disabled={!!busy}
                      onClick={() => change(a.id, "Shortlisted")}
                    >
                      Shortlist
                    </button>
                    <button
                      className="btn small primary"
                      disabled={!!busy}
                      onClick={() => change(a.id, "Booked")}
                    >
                      Book
                    </button>
                    <button
                      className="link danger"
                      disabled={!!busy}
                      onClick={() => change(a.id, "Declined")}
                    >
                      Decline
                    </button>
                  </>
                )}
                {a.status === "Completed" && (
                  <ReviewForm
                    application={a}
                    onSaved={() => {
                      setVersion((v) => v + 1);
                      onChanged();
                    }}
                  />
                )}
              </div>
            </div>
          ))
      )}
    </>
  );
}
function ReviewForm({
  application: a,
  onSaved,
}: {
  application: Application;
  onSaved: () => void;
}) {
  const [open, setOpen] = useState(false);
  if (!open)
    return (
      <button className="btn small primary" onClick={() => setOpen(true)}>
        Leave a review
      </button>
    );
  return (
    <div className="inline-review">
      <ActionForm
        submitLabel="Publish review"
        onSubmit={async (f) => {
          await api("/reviews", "POST", {
            applicationId: a.id,
            rating: Number(f.get("rating")),
            text: f.get("text"),
          });
          setOpen(false);
          onSaved();
        }}
      >
        <Field label="Your rating">
          <select name="rating">
            {[5, 4, 3, 2, 1].map((x) => (
              <option key={x} value={x}>
                {x} out of 5
              </option>
            ))}
          </select>
        </Field>
        <Field label="How was working together?">
          <textarea required minLength={10} maxLength={1500} name="text" />
        </Field>
      </ActionForm>
    </div>
  );
}
