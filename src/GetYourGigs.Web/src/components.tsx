import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";
import {
  ArrowUpRight,
  Bookmark as BookmarkIcon,
  MapPin,
  CalendarDays,
  X,
  LockKeyhole,
  ChevronRight,
  LoaderCircle,
  Star,
  AlertCircle,
  Upload,
} from "lucide-react";
import { api } from "./api";
import {
  type Profile,
  type Gig,
  type Bookmark,
  initials,
  displayName,
  dateLabel,
  money,
  categories,
  categoryIcons,
} from "./types";
export function Brand() {
  return (
    <span className="brand">
      Get Your Gigs<span className="brand-petals">′′</span>
    </span>
  );
}
export function Avatar({ name, photo }: { name: string; photo?: string }) {
  return photo ? (
    <img className="avatar" src={photo} alt="" />
  ) : (
    <span className="avatar">{initials(name)}</span>
  );
}
export function Status({ value }: { value: string }) {
  return (
    <span className={"status status-" + value.toLowerCase()}>
      {value === "NotSelected" ? "Not selected" : value}
    </span>
  );
}
export function ErrorBox({ message }: { message: string }) {
  return message ? (
    <div role="alert" className="error-box">
      <AlertCircle size={17} />
      {message}
    </div>
  ) : null;
}
export function Loading() {
  return (
    <div className="loading">
      <LoaderCircle size={23} className="spin" /> Finding the good stuff…
    </div>
  );
}
export function Empty({
  title,
  children,
  action,
}: {
  title: string;
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="empty">
      <span className="empty-art">✳</span>
      <h3>{title}</h3>
      <p>{children}</p>
      {action}
    </div>
  );
}
export function Modal({
  children,
  onClose,
  wide = false,
}: {
  children: ReactNode;
  onClose: () => void;
  wide?: boolean;
}) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const prior = document.activeElement as HTMLElement | null;
    const old = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    ref.current?.focus();
    const key = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "Tab") {
        const els = ref.current?.querySelectorAll<HTMLElement>(
          'button:not(:disabled),a,input:not(:disabled),textarea,select,[tabindex="0"]',
        );
        if (!els?.length) return;
        const first = els[0],
          last = els[els.length - 1];
        if (
          e.shiftKey &&
          (document.activeElement === first ||
            document.activeElement === ref.current)
        ) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };
    document.addEventListener("keydown", key);
    return () => {
      document.body.style.overflow = old;
      document.removeEventListener("keydown", key);
      prior?.focus();
    };
  }, [onClose]);
  return (
    <div
      className="modal-overlay"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        ref={ref}
        tabIndex={-1}
        className={"modal " + (wide ? "wide" : "")}
        role="dialog"
        aria-modal="true"
        aria-label="Get Your Gigs dialog"
      >
        <button className="close" aria-label="Close dialog" onClick={onClose}>
          <X size={20} />
        </button>
        {children}
      </div>
    </div>
  );
}
export function ActionForm({
  onSubmit,
  children,
  submitLabel = "Save",
  className = "",
}: {
  onSubmit: (form: FormData) => Promise<void>;
  children?: ReactNode;
  submitLabel?: string;
  className?: string;
}) {
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setBusy(true);
    setError("");
    try {
      await onSubmit(form);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className={className} onSubmit={submit}>
      {children}
      <ErrorBox message={error} />
      <button className="btn primary full" disabled={busy}>
        {busy ? <LoaderCircle className="spin" size={17} /> : null}
        {busy ? "One moment…" : submitLabel}
      </button>
    </form>
  );
}
export function Field({
  label,
  children,
  hint,
}: {
  label: string;
  children: ReactNode;
  hint?: string;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
      {hint && <small>{hint}</small>}
    </label>
  );
}
export function CategorySelect({
  value,
  onChange,
  name = "category",
}: {
  value?: string;
  onChange?: (value: string) => void;
  name?: string;
}) {
  return (
    <select
      name={name}
      value={value}
      onChange={(e) => onChange?.(e.target.value)}
      required
    >
      <option value="">Choose a category</option>
      {categories.map((x) => (
        <option key={x}>{x}</option>
      ))}
    </select>
  );
}
export function CategoryTiles({
  selected,
  onSelect,
}: {
  selected: string;
  onSelect: (v: string) => void;
}) {
  return (
    <div className="category-tiles">
      {categories.map((x, i) => (
        <button
          key={x}
          className={
            "category-tile tile-" + i + (selected === x ? " selected" : "")
          }
          onClick={() => onSelect(selected === x ? "" : x)}
        >
          <span>{categoryIcons[i]}</span>
          <small>{x}</small>
        </button>
      ))}
    </div>
  );
}
function SaveButton({
  type,
  id,
  bookmarks,
  onSave,
}: {
  type: "profile" | "gig";
  id: string;
  bookmarks: Bookmark[];
  onSave: (type: "profile" | "gig", id: string) => void;
}) {
  const saved = bookmarks.some(
    (x) => x.targetType === type && x.targetId === id,
  );
  return (
    <button
      className={"save-button " + (saved ? "saved" : "")}
      aria-label={saved ? "Remove bookmark" : "Save for later"}
      aria-pressed={saved}
      onClick={(e) => {
        e.stopPropagation();
        onSave(type, id);
      }}
    >
      <BookmarkIcon size={18} fill={saved ? "currentColor" : "none"} />
    </button>
  );
}
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
export function PreviewGate({ onSignIn }: { onSignIn: () => void }) {
  return (
    <div className="gate">
      <div className="ghost-cards" aria-hidden="true">
        {[0, 1, 2].map((x) => (
          <div key={x}>
            <div />
            <h3>Your next opportunity</h3>
            <p>Good people. Great work.</p>
          </div>
        ))}
      </div>
      <div className="gate-content">
        <span className="lock">
          <LockKeyhole size={18} />
        </span>
        <h3>There’s more with your name on it.</h3>
        <p>
          Sign in to meet all the talent, explore every gig, and make your move.
        </p>
        <button className="btn primary" onClick={onSignIn}>
          Join the community <ArrowUpRight size={16} />
        </button>
        <small>One account. Both sides of the stage.</small>
      </div>
    </div>
  );
}
export function PhotoInput({
  photos,
  onChange,
}: {
  photos: string[];
  onChange: (v: string[]) => void;
}) {
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  return (
    <div className="field">
      <span>
        Photos <small>optional · up to 5</small>
      </span>
      <div className="photo-input">
        {photos.map((url, i) => (
          <div className="photo-thumb" key={url}>
            <img src={url} alt={"Photo " + (i + 1)} />
            <button
              type="button"
              aria-label="Remove photo"
              onClick={() => onChange(photos.filter((_, j) => j !== i))}
            >
              <X size={12} />
            </button>
          </div>
        ))}
        {photos.length < 5 && (
          <label className="upload-tile">
            <Upload size={19} />
            <small>{busy ? "Uploading…" : "Add a photo"}</small>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              disabled={busy}
              onChange={async (e) => {
                const f = e.target.files?.[0];
                if (!f) return;
                setBusy(true);
                setError("");
                try {
                  const body = new FormData();
                  body.append("file", f);
                  const result = await api<{ url: string }>(
                    "/uploads",
                    "POST",
                    body,
                  );
                  onChange([...photos, result.url]);
                } catch (e) {
                  setError((e as Error).message);
                } finally {
                  setBusy(false);
                }
              }}
            />
          </label>
        )}
      </div>
      <ErrorBox message={error} />
    </div>
  );
}
