import { useEffect, useState } from "react";
import {
  ArrowRight,
  ArrowUpRight,
  Bell,
  CalendarDays,
  CheckCheck,
  Heart,
  MapPin,
  MessageCircle,
  Plus,
  RefreshCw,
  Search,
  Send,
  SlidersHorizontal,
  Sparkles,
} from "lucide-react";
import { api, useDebounce, useResource } from "./api";
import {
  Avatar,
  CategoryTiles,
  Empty,
  ErrorBox,
  GigCard,
  Loading,
  PreviewGate,
  Status,
  TalentCard,
} from "./components";
import {
  type Account,
  type Mode,
  type Profile,
  type Gig,
  type Application,
  type Page,
  type Bookmark,
  type Thread,
  type Message,
  type Activity,
  displayName,
  dateLabel,
} from "./types";
export interface ListingActions {
  onProfile: (p: Profile) => void;
  onGig: (g: Gig) => void;
  onSave: (type: "profile" | "gig", id: string) => void;
  bookmarks: Bookmark[];
}
export function Browse({
  user,
  mode,
  revision,
  onSignIn,
  onPost,
  ...actions
}: {
  user: Account | null;
  mode: Mode;
  revision: number;
  onSignIn: () => void;
  onPost: () => void;
} & ListingActions) {
  const [q, setQ] = useState(""),
    [category, setCategory] = useState(""),
    [city, setCity] = useState(""),
    [available, setAvailable] = useState(false),
    [page, setPage] = useState(1);
  const query = useDebounce(q),
    cityQuery = useDebounce(city);
  const hiring = mode === "Hiring";
  const params = new URLSearchParams({
    q: query,
    category,
    city: cityQuery,
    page: String(page),
    ...(available && hiring ? { available: "true" } : {}),
  });
  const { data, loading, error } = useResource<Page<Profile | Gig>>(
    "/" + (hiring ? "profiles" : "gigs") + "?" + params,
    revision,
  );
  const allowance = useResource<{ used: number; limit: number }>(
    user && !hiring ? "/me/allowance" : null,
    revision,
  );
  useEffect(() => setPage(1), [query, category, cityQuery, mode, available]);
  return (
    <>
      {!user ? (
        <>
          <section className="hero">
            <div className="hero-copy">
              <span className="eyebrow">
                <i /> PEOPLE. SKILLS. OPPORTUNITIES.
              </span>
              <h1>
                {hiring ? (
                  <>
                    Big ideas.
                    <br />
                    Meet your <em>people.</em>
                  </>
                ) : (
                  <>
                    Real skills.
                    <br />
                    Meet your <em>next gig.</em>
                  </>
                )}
              </h1>
              <p>
                {hiring
                  ? "From your big day to your next big thing. Find independent talent who bring something special."
                  : "A stage for what you do best. Find meaningful gigs, meet good people, and make your next move."}
              </p>
              <button
                className="btn primary"
                onClick={() =>
                  document
                    .getElementById("discovery")
                    ?.scrollIntoView({ behavior: "smooth" })
                }
              >
                {hiring ? "Find your people" : "Find my next gig"}{" "}
                <ArrowUpRight size={17} />
              </button>
              <div className="hero-note">
                <span className="little-spark">✳</span> Good people. Great work.
                No labels.
              </div>
            </div>
            <div className="hero-visual">
              <div className="orb orb-one" />
              <div className="orb orb-two" />
              <img
                src="https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=800&q=85"
                alt="Musician performing on stage"
              />
              <div className="hero-sticker">
                <span>♫</span>
                <div>
                  <strong>
                    A little talent.
                    <br />A whole lot of possibility.
                  </strong>
                  <small>MAKE YOUR NEXT GREAT CONNECTION</small>
                </div>
              </div>
              <span className="visual-spark">✳</span>
              <span className="floating-note">
                <span className="online-dot" /> Your kind of people are here.
              </span>
            </div>
          </section>
          <div className="values-strip">
            <span>
              <Sparkles size={17} /> Real people, real skills
            </span>
            <span>
              <Heart size={17} /> One account, every opportunity
            </span>
            <span>
              <MessageCircle size={17} /> Connections that start with hello
            </span>
          </div>
        </>
      ) : (
        <>
          <section className="dashboard-hero">
            <div>
              <span className="eyebrow">
                {hiring
                  ? "LET’S BRING YOUR NEXT IDEA TO LIFE"
                  : "YOUR TALENT DESERVES A STAGE"}
              </span>
              <h1>
                {hiring ? (
                  <>
                    Good people.
                    <br />
                    <em>Great possibilities.</em>
                  </>
                ) : (
                  <>
                    Your next chapter
                    <br />
                    starts with <em>a gig.</em>
                  </>
                )}
              </h1>
              <p>
                {hiring
                  ? `Welcome back, ${displayName(user.name).split(" ")[0]}. Find the right people for whatever’s next.`
                  : "Thoughtful work for independent people. Find something that feels like you."}
              </p>
            </div>
            <div className="dashboard-note">
              <span>✳</span>
              <strong>
                {hiring ? "You bring the idea." : "Bring what makes you, you."}
              </strong>
              <p>
                {hiring
                  ? "They bring the magic."
                  : `${allowance.data ? allowance.data.limit - allowance.data.used : 3} applications available today.`}
              </p>
              <button
                className="link"
                onClick={
                  hiring
                    ? onPost
                    : () =>
                        document
                          .getElementById("discovery")
                          ?.scrollIntoView({ behavior: "smooth" })
                }
              >
                {hiring ? "Post your brief" : "Explore open gigs"}{" "}
                <ArrowRight size={15} />
              </button>
            </div>
          </section>
        </>
      )}
      <section className="discovery" id="discovery">
        <div className="section-head">
          <div>
            <span className="eyebrow">
              {hiring
                ? "FIND YOUR NEXT GREAT COLLABORATOR"
                : "A LITTLE POSSIBILITY, EVERY DAY"}
            </span>
            <h2>
              {hiring
                ? "There’s a person for that."
                : "Good gigs. Great beginnings."}
            </h2>
            <p>
              {hiring
                ? "Independent talent. Exceptional at what they do."
                : "Explore opportunities that match your kind of brilliant."}
            </p>
          </div>
          <span className="small-caption">
            A marketplace for the way you work <ArrowUpRight size={14} />
          </span>
        </div>
        <CategoryTiles selected={category} onSelect={setCategory} />
        <div className="search-toolbar">
          <label className="search-field">
            <Search size={18} />
            <input
              aria-label={hiring ? "Search talent" : "Search gigs"}
              placeholder={
                hiring
                  ? "Try “photographer”, “live band”, or “designer”…"
                  : "What kind of gig are you looking for?"
              }
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </label>
          <label className="location-filter">
            <MapPin size={16} />
            <input
              aria-label="Filter by city"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              placeholder="Any location"
            />
          </label>
          {hiring && (
            <button
              className={"filter-button " + (available ? "active" : "")}
              aria-pressed={available}
              onClick={() => setAvailable(!available)}
            >
              <SlidersHorizontal size={15} />
              {available ? "Available now" : "Availability"}
            </button>
          )}
        </div>
        <div className="results-heading">
          <span>
            {data ? (
              <>
                <strong>{data.total}</strong>{" "}
                {hiring ? "talented people" : "open opportunities"}
                {category ? " in " + category : ""}
              </>
            ) : (
              "Finding your next connection…"
            )}
          </span>
          <span>
            {user
              ? "Made for independent work"
              : "A little preview. A lot more inside."}
          </span>
        </div>
        <ErrorBox message={error} />
        {loading ? (
          <Loading />
        ) : data?.items.length ? (
          <div className={hiring ? "talent-grid" : "gig-grid"}>
            {data.items.map((item) =>
              hiring ? (
                <TalentCard
                  key={item.id}
                  profile={item as Profile}
                  onOpen={actions.onProfile}
                  onSave={actions.onSave}
                  bookmarks={actions.bookmarks}
                />
              ) : (
                <GigCard
                  key={item.id}
                  gig={item as Gig}
                  onOpen={actions.onGig}
                  onSave={actions.onSave}
                  bookmarks={actions.bookmarks}
                />
              ),
            )}
          </div>
        ) : !error ? (
          <Empty title="Nothing here just yet.">
            Try another category or location. Your next connection might be one
            search away.
          </Empty>
        ) : null}
        {!user && <PreviewGate onSignIn={onSignIn} />}{" "}
        {user && data && data.total > data.pageSize && (
          <div className="pagination">
            <button
              className="btn outline small"
              disabled={page <= 1}
              onClick={() => setPage(page - 1)}
            >
              Previous
            </button>
            <span>
              Page {page} of {Math.ceil(data.total / data.pageSize)}
            </span>
            <button
              className="btn outline small"
              disabled={page * data.pageSize >= data.total}
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        )}
      </section>
    </>
  );
}
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
    setItems([]);
    setLoading(true);
    setError("");
    Promise.all(
      bookmarks
        .filter((x) => x.targetType === type)
        .map((x) =>
          api<Profile | Gig>(
            "/" + (type === "profile" ? "profiles" : "gigs") + "/" + x.targetId,
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
export function Inbox({
  user,
  initialThread,
  revision,
}: {
  user: Account;
  initialThread?: string;
  revision: number;
}) {
  const [selected, setSelected] = useState(initialThread || ""),
    [tick, setTick] = useState(0),
    [text, setText] = useState(""),
    [sending, setSending] = useState(false),
    [error, setError] = useState("");
  const threads = useResource<Thread[]>("/threads", revision + tick);
  const messages = useResource<Message[]>(
    selected ? "/threads/" + selected + "/messages" : null,
    revision + tick,
  );
  useEffect(() => {
    const id = setInterval(() => {
      if (!document.hidden) setTick((x) => x + 1);
    }, 10000);
    return () => clearInterval(id);
  }, []);
  useEffect(() => {
    if (initialThread) setSelected(initialThread);
  }, [initialThread]);
  useEffect(() => {
    if (!selected && threads.data?.length) setSelected(threads.data[0].id);
  }, [threads.data, selected]);
  const current = threads.data?.find((x) => x.id === selected);
  return (
    <section className="page">
      <div className="section-head">
        <div>
          <span className="eyebrow">GOOD WORK STARTS WITH A CONVERSATION</span>
          <h1>Your inbox.</h1>
          <p>
            Leave a message. Pick up where you left off. No need to be online
            together.
          </p>
        </div>
        <button
          className="btn outline small"
          onClick={() => setTick((x) => x + 1)}
        >
          <RefreshCw size={14} />
          Refresh
        </button>
      </div>
      <ErrorBox message={threads.error || messages.error || error} />
      {threads.loading && !threads.data ? (
        <Loading />
      ) : !threads.data?.length ? (
        <Empty title="Your next connection starts with hello.">
          Message someone from their profile or a gig to start a conversation.
        </Empty>
      ) : (
        <div className="inbox">
          <aside className="thread-list">
            {threads.data.map((t) => (
              <button
                key={t.id}
                className={"thread-item " + (selected === t.id ? "active" : "")}
                onClick={() => {
                  setSelected(t.id);
                  setText("");
                }}
              >
                <Avatar name={t.otherName} />
                <div>
                  <strong>{displayName(t.otherName)}</strong>
                  <small>{t.lastMessage}</small>
                </div>
                {t.unreadCount > 0 && (
                  <span className="unread-count">{t.unreadCount}</span>
                )}
              </button>
            ))}
          </aside>
          <div className="chat">
            <div className="chat-header">
              <Avatar name={current?.otherName || "Conversation"} />
              <div>
                <strong>
                  {displayName(current?.otherName || "Conversation")}
                </strong>
                <small>
                  {current?.gigTitle
                    ? displayName(current.gigTitle)
                    : "A new connection"}
                </small>
              </div>
              <span className="async-label">ASYNCHRONOUS MESSAGING</span>
            </div>
            <div className="chat-messages">
              {messages.loading && !messages.data ? (
                <Loading />
              ) : (
                messages.data?.map((m) => (
                  <div
                    key={m.id}
                    className={
                      "message " + (m.senderId === user.id ? "mine" : "")
                    }
                  >
                    <p>{m.text}</p>
                    <time>
                      {new Date(m.createdAt).toLocaleString("en-IN", {
                        day: "numeric",
                        month: "short",
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </time>
                  </div>
                ))
              )}
            </div>
            <form
              className="chat-compose"
              onSubmit={async (e) => {
                e.preventDefault();
                if (!text.trim() || !selected) return;
                setSending(true);
                setError("");
                try {
                  await api("/threads/" + selected + "/messages", "POST", {
                    text,
                  });
                  setText("");
                  setTick((x) => x + 1);
                } catch (e) {
                  setError((e as Error).message);
                } finally {
                  setSending(false);
                }
              }}
            >
              <input
                required
                maxLength={3000}
                aria-label="Your reply"
                value={text}
                onChange={(e) => setText(e.target.value)}
                placeholder="A little hello goes a long way…"
              />
              <button
                className="btn primary"
                disabled={sending || !text.trim()}
                aria-label="Send reply"
              >
                <Send size={17} />
              </button>
            </form>
          </div>
        </div>
      )}
    </section>
  );
}
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
