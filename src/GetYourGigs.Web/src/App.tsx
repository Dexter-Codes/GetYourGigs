import { Menu, Plus, X } from "lucide-react";
import { api } from "./api";

import { Avatar, Brand, ErrorBox, Loading, Modal } from "./components";

import { Browse, Inbox, MyGigs, Saved, Updates } from "./pages";
import { ModeSwitcher } from "./components/ModeSwitcher";
import { nav } from "./app/navigation";
import { AppDialog } from "./app/AppDialog";
import { useMarketplace } from "./app/useMarketplace";

export default function App() {
  const marketplace = useMarketplace();
  const {
    account,
    view,
    dialog,
    setDialog,
    revision,
    toast,
    mobileNav,
    setMobileNav,
    initialThread,
    restoring,
    config,
    mode,
    close,
    refresh,
    go,
    changeMode,
    post,
    listing,
  } = marketplace;
  const content = restoring ? (
    <Loading />
  ) : !account || view === "discover" ? (
    <Browse
      user={account}
      mode={mode}
      revision={revision}
      onSignIn={() => setDialog({ kind: "signin" })}
      onPost={post}
      {...listing}
    />
  ) : view === "gigs" ? (
    <MyGigs
      user={account}
      mode={mode}
      revision={revision}
      onPost={post}
      onBrowse={() => go("discover")}
      onWithdraw={(id) =>
        setDialog({
          kind: "confirm",
          title: "Withdraw from this gig?",
          text: "The hirer will be notified. Withdrawing does not restore today’s application allowance.",
          action: async () => {
            await api("/applications/" + id + "/withdraw", "POST");
          },
        })
      }
      {...listing}
    />
  ) : view === "saved" ? (
    <Saved mode={mode} revision={revision} {...listing} />
  ) : view === "inbox" ? (
    <Inbox user={account} initialThread={initialThread} revision={revision} />
  ) : (
    <Updates revision={revision} onGig={listing.onGig} />
  );
  return (
    <>
      <a className="skip-link" href="#main-content" inert={Boolean(dialog)}>
        Skip to main content
      </a>
      <header className="topbar" inert={Boolean(dialog)}>
        <div className="header-inner">
          <button
            className="brand-button"
            onClick={() => go("discover")}
            aria-label="Get Your Gigs home"
          >
            <Brand />
          </button>
          <nav className="desktop-nav" aria-label="Main navigation">
            {nav.map((n) => (
              <button
                key={n.id}
                className={view === n.id ? "active" : ""}
                aria-current={view === n.id ? "page" : undefined}
                onClick={() => go(n.id)}
              >
                <n.icon size={18} aria-hidden="true" />
                {n.label}
              </button>
            ))}
          </nav>
          <div className="header-actions">
            {account ? (
              <>
                <button
                  className="avatar-button"
                  aria-label="Your account"
                  onClick={() => setDialog({ kind: "account" })}
                >
                  <Avatar name={account.name} />
                  <span>Account</span>
                </button>
              </>
            ) : (
              <>
                <button
                  className="link signin-link"
                  onClick={() => setDialog({ kind: "signin" })}
                >
                  Sign in
                </button>
                <button className="btn primary small" onClick={post}>
                  Post a gig <Plus size={15} />
                </button>
              </>
            )}
            <button
              className="mobile-menu icon-button"
              aria-label={mobileNav ? "Close navigation" : "Open navigation"}
              aria-controls="mobile-navigation"
              aria-expanded={mobileNav}
              onClick={() => setMobileNav(!mobileNav)}
            >
              {mobileNav ? <X size={21} /> : <Menu size={21} />}
            </button>
          </div>
        </div>
        {mobileNav && (
          <nav
            id="mobile-navigation"
            className="mobile-dropdown"
            aria-label="Main navigation"
          >
            {nav.map((n) => (
              <button
                key={n.id}
                aria-current={view === n.id ? "page" : undefined}
                onClick={() => go(n.id)}
              >
                <n.icon size={17} />
                {n.label}
              </button>
            ))}
          </nav>
        )}
      </header>
      <main
        id="main-content"
        tabIndex={-1}
        className="container"
        inert={Boolean(dialog)}
      >
        <ModeSwitcher mode={mode} onChange={changeMode} />
        {config.error && (
          <div className="api-error">
            <ErrorBox
              message={"The marketplace is unavailable: " + config.error}
            />
            <button className="btn outline small" onClick={refresh}>
              Try again
            </button>
          </div>
        )}
        {content}
      </main>
      <footer className="footer container" inert={Boolean(dialog)}>
        <Brand />
        <span>For people who make things happen.</span>
        <span className="footer-note">Built on real connections. Always.</span>
      </footer>
      {account && (
        <nav
          className="bottom-nav"
          aria-label="Mobile navigation"
          inert={Boolean(dialog)}
        >
          {nav.map((n) => (
            <button
              key={n.id}
              className={view === n.id ? "active" : ""}
              aria-current={view === n.id ? "page" : undefined}
              onClick={() => go(n.id)}
            >
              <n.icon size={20} />
              <span>{n.label}</span>
            </button>
          ))}
        </nav>
      )}
      {dialog && (
        <Modal
          label={
            dialog.kind === "confirm" || dialog.kind === "success"
              ? dialog.title
              : {
                  signin: "Sign in or create an account",
                  profile: "Talent profile",
                  gig: "Gig details",
                  profileForm: "Your talent profile",
                  post: "Post or edit a gig",
                  apply: "Apply to this gig",
                  applicants: "Gig applicants",
                  message: "New message",
                  account: "Your account",
                }[dialog.kind]
          }
          onClose={close}
          wide={["profile", "gig", "applicants"].includes(dialog.kind)}
        >
          {<AppDialog marketplace={marketplace} />}
        </Modal>
      )}
      {toast && (
        <div className="toast" role="status">
          {toast}
        </div>
      )}
    </>
  );
}
