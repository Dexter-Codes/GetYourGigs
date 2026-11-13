import { useEffect, useRef, useState } from "react";
import {
  ArrowRight,
  Check,
  CheckCircle2,
  Mail,
  MapPin,
  Star,
} from "lucide-react";
import { api, setToken, useResource } from "./api";
import {
  ActionForm,
  Avatar,
  Brand,
  CategorySelect,
  CategoryTiles,
  Empty,
  ErrorBox,
  Field,
  Loading,
  PhotoInput,
  Status,
} from "./components";
import {
  type Account,
  type Auth,
  type Config,
  type Profile,
  type ProfileInput,
  type Gig,
  type GigInput,
  type Application,
  type Review,
  type Thread,
  categories,
  displayName,
  dateLabel,
  money,
} from "./types";
interface GoogleApi {
  accounts: {
    id: {
      initialize: (o: {
        client_id: string;
        callback: (r: { credential: string }) => void;
      }) => void;
      renderButton: (el: HTMLElement, o: object) => void;
    };
  };
}
declare global {
  interface Window {
    google?: GoogleApi;
  }
}
export function SignIn({
  config,
  onSignedIn,
}: {
  config: Config;
  onSignedIn: (a: Account) => void;
}) {
  const [email, setEmail] = useState(""),
    [sent, setSent] = useState(false),
    [devCode, setDevCode] = useState<string | null>(null),
    [googleError, setGoogleError] = useState("");
  const googleRef = useRef<HTMLDivElement>(null);
  function signed(a: Auth) {
    setToken(a.accessToken);
    onSignedIn(a.account);
  }
  useEffect(() => {
    if (!config.googleClientId) return;
    let active = true;
    const init = () => {
      if (!active || !window.google || !googleRef.current) return;
      window.google.accounts.id.initialize({
        client_id: config.googleClientId!,
        callback: (r) => {
          api<Auth>("/auth/google", "POST", { idToken: r.credential })
            .then(signed)
            .catch((e) => setGoogleError(e.message));
        },
      });
      window.google.accounts.id.renderButton(googleRef.current, {
        theme: "outline",
        size: "large",
        width: 360,
      });
    };
    if (window.google) init();
    else {
      let script =
        document.querySelector<HTMLScriptElement>("#google-identity");
      if (!script) {
        script = document.createElement("script");
        script.id = "google-identity";
        script.src = "https://accounts.google.com/gsi/client";
        script.async = true;
        document.head.appendChild(script);
      }
      script.addEventListener("load", init);
      script.addEventListener("error", () =>
        setGoogleError(
          "Google sign-in is unavailable. You can use email instead.",
        ),
      );
    }
    return () => {
      active = false;
    };
  }, [config.googleClientId]);
  return (
    <>
      <Brand />
      <span className="eyebrow">YOUR NEXT CHAPTER STARTS HERE</span>
      <h2>
        A little introduction.
        <br />A lot of <em>possibility.</em>
      </h2>
      <p>
        Hire brilliant people. Find meaningful work. One account lets you do
        both.
      </p>
      {config.googleClientId && (
        <>
          <div className="google-signin" ref={googleRef} />
          <ErrorBox message={googleError} />
          <div className="divider">or use your email</div>
        </>
      )}
      {!sent ? (
        <ActionForm
          submitLabel="Send my sign-in code"
          onSubmit={async () => {
            const r = await api<{
              message: string;
              developmentCode: string | null;
            }>("/auth/email/code", "POST", { email });
            setDevCode(r.developmentCode);
            setSent(true);
          }}
        >
          <Field label="Email address">
            <input
              type="email"
              required
              maxLength={254}
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
            />
          </Field>
        </ActionForm>
      ) : (
        <>
          <div className="notice">
            <Mail size={17} />
            <span>
              Enter the code sent to <strong>{email}</strong>. It expires in 10
              minutes.
            </span>
          </div>
          {devCode && (
            <div className="development-note">
              <strong>Local development email</strong>
              <br />
              No email was sent. Your test code is <b>{devCode}</b>. SMTP
              delivery is required in production.
            </div>
          )}
          <ActionForm
            submitLabel="Let’s get you in"
            onSubmit={async (f) =>
              signed(
                await api<Auth>("/auth/email/verify", "POST", {
                  email,
                  code: f.get("code"),
                  name: f.get("name"),
                }),
              )
            }
          >
            <Field label="Your name">
              <input
                name="name"
                required
                minLength={2}
                maxLength={80}
                autoComplete="name"
                placeholder="How should we call you?"
              />
            </Field>
            <Field label="Six-digit code">
              <input
                name="code"
                required
                pattern="[0-9]{6}"
                inputMode="numeric"
                maxLength={6}
                autoComplete="one-time-code"
                placeholder="000000"
              />
            </Field>
          </ActionForm>
          <button
            className="link centered"
            onClick={() => {
              setSent(false);
              setDevCode(null);
            }}
          >
            Use a different email / resend code
          </button>
        </>
      )}
      <p className="fine-print">
        No separate talent or hirer accounts. Switch sides whenever you like.
      </p>
    </>
  );
}
const blankProfile: ProfileInput = {
  headline: "",
  category: "",
  city: "",
  rate: 0,
  bio: "",
  skills: [],
  photos: [],
  rateUnit: "per gig",
  available: true,
};
export function ProfileForm({
  existing,
  onSaved,
  onPublished,
}: {
  existing?: Profile | null;
  onSaved: () => void;
  onPublished: () => void;
}) {
  const [form, setForm] = useState<ProfileInput>(
      existing ? { ...existing } : blankProfile,
    ),
    [step, setStep] = useState(0),
    [live, setLive] = useState(false);
  const set = <K extends keyof ProfileInput>(key: K, v: ProfileInput[K]) =>
    setForm((f) => ({ ...f, [key]: v }));
  if (live)
    return (
      <div className="success-screen">
        <span className="success-symbol">
          <CheckCircle2 size={40} />
        </span>
        <span className="eyebrow">YOUR TALENT, OUT IN THE WORLD</span>
        <h2>
          You’re <em>live.</em>
        </h2>
        <p>
          Your listing is published immediately. Good opportunities start with
          showing up.
        </p>
        <div className="notice">
          Complete your portfolio and skills later from your profile.
        </div>
        <button className="btn primary full" onClick={onSaved}>
          Find my next gig <ArrowRight size={16} />
        </button>
      </div>
    );
  const titles = [
    "What do you do best?",
    "Where’s your home base?",
    "What’s your starting rate?",
    "What’s in your toolkit?",
    "Tell your story.",
  ];
  return (
    <>
      <span className="eyebrow">
        {existing ? "YOUR TALENT PROFILE" : "FIVE QUESTIONS. THEN YOU’RE LIVE."}
      </span>
      <h2>{existing ? "Make it yours." : titles[step]}</h2>
      {!existing && (
        <>
          <div className="stepper">
            {titles.map((_, i) => (
              <span key={i} className={i <= step ? "done" : ""} />
            ))}
          </div>
          <p>Question {step + 1} of 5 · you can edit everything later</p>
        </>
      )}
      <ActionForm
        key={step}
        submitLabel={
          existing
            ? "Save profile"
            : step === 4
              ? "Publish my profile"
              : "Continue"
        }
        onSubmit={async () => {
          if (!existing && step < 4) {
            setStep(step + 1);
            return;
          }
          await api("/me/profile", "PUT", form);
          onPublished();
          if (existing) onSaved();
          else setLive(true);
        }}
      >
        {(existing || step === 0) && (
          <>
            <Field label="Your professional headline">
              <input
                required
                minLength={3}
                maxLength={100}
                value={form.headline}
                onChange={(e) => set("headline", e.target.value)}
                placeholder="e.g. Acoustic musician & live performer"
              />
            </Field>
            <Field label="Your category">
              <CategorySelect
                value={form.category}
                onChange={(v) => set("category", v)}
              />
            </Field>
          </>
        )}
        {(existing || step === 1) && (
          <Field label="City or service area">
            <input
              required
              minLength={2}
              maxLength={80}
              value={form.city}
              onChange={(e) => set("city", e.target.value)}
              placeholder="e.g. Mumbai, Bengaluru, or Remote"
            />
          </Field>
        )}
        {(existing || step === 2) && (
          <div className="field-row">
            <Field
              label="Starting rate (₹)"
              hint="Use 0 to discuss pricing directly."
            >
              <input
                type="number"
                required
                min={0}
                max={10000000}
                value={form.rate}
                onChange={(e) => set("rate", Number(e.target.value))}
              />
            </Field>
            <Field label="Rate unit">
              <select
                value={form.rateUnit}
                onChange={(e) => set("rateUnit", e.target.value)}
              >
                {["per gig", "per hour", "per day", "per project"].map((x) => (
                  <option key={x}>{x}</option>
                ))}
              </select>
            </Field>
          </div>
        )}
        {(existing || step === 3) && (
          <Field label="Skills" hint="Separate skills with commas; up to 15.">
            <input
              maxLength={700}
              value={form.skills.join(",")}
              onChange={(e) =>
                set("skills", e.target.value.split(",").slice(0, 15))
              }
              placeholder="Live music, acoustic guitar, weddings"
            />
          </Field>
        )}
        {(existing || step === 4) && (
          <Field label="A little about you">
            <textarea
              required
              minLength={20}
              maxLength={3000}
              value={form.bio}
              onChange={(e) => set("bio", e.target.value)}
              placeholder="Your experience, your approach, and what makes working with you special…"
            />
          </Field>
        )}
        {existing && (
          <>
            <PhotoInput
              photos={form.photos}
              onChange={(p) => set("photos", p)}
            />
            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.available}
                onChange={(e) => set("available", e.target.checked)}
              />{" "}
              Available for new gigs
            </label>
          </>
        )}
      </ActionForm>
      {!existing && step > 0 && (
        <button className="link centered" onClick={() => setStep(step - 1)}>
          Back
        </button>
      )}
    </>
  );
}
export function PostGig({
  existing,
  onSaved,
}: {
  existing?: Gig;
  onSaved: () => void;
}) {
  const [step, setStep] = useState(0),
    [form, setForm] = useState<GigInput>(
      existing
        ? { ...existing }
        : {
            title: "",
            category: "",
            city: "",
            description: "",
            date: null,
            budget: 0,
            photos: [],
          },
    );
  const set = <K extends keyof GigInput>(key: K, v: GigInput[K]) =>
    setForm((f) => ({ ...f, [key]: v }));
  return (
    <>
      <span className="eyebrow">MAKE ROOM FOR SOMETHING GREAT</span>
      <h2>{existing ? "Edit your gig" : "Post a gig"}</h2>
      <p>
        Step {step + 1} of 3 ·{" "}
        {
          [
            "What do you need?",
            "Let’s get into the details.",
            "One last look.",
          ][step]
        }
      </p>
      <div className="stepper">
        {[0, 1, 2].map((i) => (
          <span key={i} className={i <= step ? "done" : ""} />
        ))}
      </div>
      <ActionForm
        key={step}
        submitLabel={
          step === 2 ? (existing ? "Save changes" : "Post my gig") : "Next"
        }
        onSubmit={async () => {
          if (step < 2) {
            if (step === 0 && !form.category)
              throw new Error("Choose a category to continue.");
            setStep(step + 1);
            return;
          }
          await api(
            "/gigs" + (existing ? "/" + existing.id : ""),
            existing ? "PUT" : "POST",
            form,
          );
          onSaved();
        }}
      >
        {step === 0 && (
          <>
            <CategoryTiles
              selected={form.category}
              onSelect={(v) => set("category", v)}
            />
            <Field label="Give your gig a title">
              <input
                required
                minLength={5}
                maxLength={120}
                placeholder="e.g. Live band for an intimate wedding"
                value={form.title}
                onChange={(e) => set("title", e.target.value)}
              />
            </Field>
          </>
        )}
        {step === 1 && (
          <>
            <Field label="Location">
              <input
                required
                minLength={2}
                maxLength={80}
                value={form.city}
                onChange={(e) => set("city", e.target.value)}
                placeholder="City, venue area, or Remote"
              />
            </Field>
            <div className="field-row">
              <Field label="Date (optional)">
                <input
                  type="date"
                  value={form.date || ""}
                  onChange={(e) => set("date", e.target.value || null)}
                />
              </Field>
              <Field label="Budget (₹)" hint="0 = open to discussing">
                <input
                  type="number"
                  required
                  min={0}
                  max={10000000}
                  value={form.budget}
                  onChange={(e) => set("budget", Number(e.target.value))}
                />
              </Field>
            </div>
            <Field label="What should talent know?">
              <textarea
                required
                minLength={20}
                maxLength={4000}
                value={form.description}
                onChange={(e) => set("description", e.target.value)}
                placeholder="The event, the brief, the kind of person you’re looking for…"
              />
            </Field>
            <PhotoInput
              photos={form.photos}
              onChange={(v) => set("photos", v)}
            />
          </>
        )}
        {step === 2 && (
          <div className="review-summary">
            <span className="category-label">{form.category}</span>
            <h3>{form.title}</h3>
            <p>
              <MapPin size={15} /> {form.city} · {dateLabel(form.date)}
            </p>
            <h4>
              {form.budget ? money(form.budget) : "Budget to be discussed"}
            </h4>
            <p>{form.description}</p>
            <div className="notice">
              <Check size={16} />
              Your gig will be visible to talent immediately.
            </div>
          </div>
        )}
      </ActionForm>
      {step > 0 && (
        <button className="link centered" onClick={() => setStep(step - 1)}>
          Back
        </button>
      )}
    </>
  );
}
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
export function ApplyGig({ gig, onSent }: { gig: Gig; onSent: () => void }) {
  const allowance = useResource<{ used: number; limit: number }>(
    "/me/allowance",
  );
  return (
    <>
      <span className="eyebrow">GIVE IT YOUR BEST SHOT</span>
      <h2>
        Your next gig
        <br />
        could be <em>this one.</em>
      </h2>
      <p>
        {displayName(gig.title)} · {gig.city}
      </p>
      <div className="notice">
        {allowance.data
          ? `${allowance.data.limit - allowance.data.used} of 3 applications left today`
          : "Up to 3 thoughtful applications per day."}
      </div>
      <ActionForm
        submitLabel="Send my application"
        onSubmit={async (f) => {
          await api("/gigs/" + gig.id + "/applications", "POST", {
            message: f.get("message"),
            expectedPrice: f.get("price") ? Number(f.get("price")) : null,
          });
          onSent();
        }}
      >
        <Field label="Why are you a great fit?">
          <textarea
            name="message"
            required
            minLength={20}
            maxLength={2000}
            placeholder="A little about your experience, availability, and what you’d bring to this gig…"
          />
        </Field>
        <Field label="Your quote (₹, optional)">
          <input
            name="price"
            type="number"
            min={0}
            max={10000000}
            placeholder="Leave blank to discuss"
          />
        </Field>
      </ActionForm>
    </>
  );
}
export function MessageForm({
  recipientId,
  recipientName,
  gigId,
  onSent,
}: {
  recipientId: string;
  recipientName: string;
  gigId?: string;
  onSent: (thread: Thread) => void;
}) {
  return (
    <>
      <span className="eyebrow">START SOMETHING GOOD</span>
      <h2>
        Say hello to
        <br />
        <em>{displayName(recipientName)}.</em>
      </h2>
      <p>
        Ask a question, share your brief, or discuss availability. They can
        reply whenever they’re free.
      </p>
      <ActionForm
        submitLabel="Send message"
        onSubmit={async (f) =>
          onSent(
            await api<Thread>("/threads", "POST", {
              recipientId,
              gigId: gigId || null,
              message: f.get("message"),
            }),
          )
        }
      >
        <Field label="Your message">
          <textarea
            required
            minLength={1}
            maxLength={3000}
            name="message"
            placeholder="Hi! I’d love to talk about…"
          />
        </Field>
      </ActionForm>
    </>
  );
}
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
