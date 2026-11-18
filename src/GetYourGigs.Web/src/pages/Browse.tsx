import { useEffect, useState } from "react";
import {
  ArrowRight,
  ArrowUpRight,
  Heart,
  MapPin,
  MessageCircle,
  Search,
  SlidersHorizontal,
  Sparkles,
} from "lucide-react";

import { useDebounce } from "../hooks/useDebounce";
import { useResource } from "../hooks/useResource";
import {
  CategoryTiles,
  Empty,
  ErrorBox,
  GigCard,
  Loading,
  PreviewGate,
  TalentCard,
} from "../components";
import {
  type Account,
  type Mode,
  type Profile,
  type Gig,
  type Page,
  displayName,
} from "../types";
import type { ListingActions } from "./ListingActions";

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
                    Find the right talent.
                    <br />
                    For your <em>next project.</em>
                  </>
                ) : (
                  <>
                    Your skills.
                    <br />
                    Your <em>next opportunity.</em>
                  </>
                )}
              </h1>
              <p>
                {hiring
                  ? "Browse people by skill, location, and availability. Discuss your project before you book."
                  : "Explore gigs across different skills and experience levels. See the details and apply when it feels right."}
              </p>
              <button
                className="btn primary"
                onClick={() =>
                  document
                    .getElementById("discovery")
                    ?.scrollIntoView({ behavior: "smooth" })
                }
              >
                {hiring ? "Browse talent" : "Browse gigs"}{" "}
                <ArrowUpRight size={17} />
              </button>
              <div className="hero-note">
                <span className="little-spark">✳</span> Hire or find work with
                the same account.
              </div>
            </div>
            <div className="hero-visual" aria-hidden="true">
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
                <span className="online-dot" /> Explore a range of skills.
              </span>
            </div>
          </section>
          <div className="values-strip">
            <span>
              <Sparkles size={17} /> Explore different skills
            </span>
            <span>
              <Heart size={17} /> One account, every opportunity
            </span>
            <span>
              <MessageCircle size={17} /> Message before you decide
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
                {hiring ? "Post a gig" : "Explore open gigs"}{" "}
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
              {hiring ? "Find talent for your project" : "Find your next gig"}
            </h2>
            <p>
              {hiring
                ? "Choose a category or search by skill and city."
                : "Choose a category or search for work near you."}
            </p>
          </div>
          <span className="small-caption">
            A marketplace for the way you work <ArrowUpRight size={14} />
          </span>
        </div>
        <CategoryTiles selected={category} onSelect={setCategory} />
        <p className="filter-help">
          Search by keyword, city, or category. Select a category again to clear
          it.
        </p>
        <div
          className="search-toolbar"
          role="search"
          aria-label={hiring ? "Find talent" : "Find gigs"}
        >
          <label className="search-field">
            <Search size={18} aria-hidden="true" />
            <span className="sr-only">Search by keyword</span>
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
            <MapPin size={16} aria-hidden="true" />
            <input
              aria-label="Filter by city"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              placeholder="Any city"
            />
          </label>
          {hiring && (
            <button
              className={"filter-button " + (available ? "active" : "")}
              aria-pressed={available}
              onClick={() => setAvailable(!available)}
            >
              <SlidersHorizontal size={15} />
              {available ? "Available now" : "Available now"}
            </button>
          )}
        </div>
        {(q || city || category || available) && (
          <button
            className="link clear-filters"
            onClick={() => {
              setQ("");
              setCity("");
              setCategory("");
              setAvailable(false);
              setPage(1);
            }}
          >
            Clear all filters
          </button>
        )}
        <div className="results-heading" role="status" aria-live="polite">
          <span>
            {data ? (
              <>
                <strong>{data.total}</strong>{" "}
                {hiring ? "talent profiles" : "open gigs"}
                {category ? " in " + category : ""}
              </>
            ) : (
              "Loading results…"
            )}
          </span>
          <span>
            {user
              ? "Made for independent work"
              : "Preview: sign in to see all results."}
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
            Try a different keyword or city, or clear your filters to see more
            results.
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
