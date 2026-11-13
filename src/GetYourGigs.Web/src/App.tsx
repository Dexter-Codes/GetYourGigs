import { useCallback, useEffect, useState } from "react";
import {
  ArrowRight,
  ArrowUpRight,
  Bell,
  Bookmark as BookmarkIcon,
  BriefcaseBusiness,
  CheckCircle2,
  Compass,
  LogOut,
  Menu,
  MessageCircle,
  Plus,
  UserRound,
  X,
} from "lucide-react";
import { api, hasToken, setToken, useResource } from "./api";
import {
  ActionForm,
  Avatar,
  Brand,
  Empty,
  ErrorBox,
  Field,
  Loading,
  Modal,
} from "./components";
import {
  Applicants,
  ApplyGig,
  GigDetail,
  MessageForm,
  PostGig,
  ProfileForm,
  SignIn,
  TalentDetail,
} from "./flows";
import { Browse, Inbox, MyGigs, Saved, Updates } from "./pages";
import {
  type Account,
  type Bookmark,
  type Config,
  type Gig,
  type Mode,
  type Profile,
  type Thread,
  categories,
  displayName,
} from "./types";
type View = "discover" | "gigs" | "inbox" | "saved" | "updates";
type Dialog =
  | { kind: "signin" }
  | { kind: "profile"; profile: Profile }
  | { kind: "gig"; gig: Gig }
  | { kind: "profileForm"; profile?: Profile | null }
  | { kind: "post"; gig?: Gig }
  | { kind: "apply"; gig: Gig }
  | { kind: "applicants"; gig: Gig }
  | { kind: "message"; recipientId: string; name: string; gigId?: string }
  | { kind: "account" }
  | { kind: "success"; title: string; text: string }
  | {
      kind: "confirm";
      title: string;
      text: string;
      action: () => Promise<void>;
    };
const nav = [
  { id: "discover" as View, label: "Discover", icon: Compass },
  { id: "gigs" as View, label: "Gigs", icon: BriefcaseBusiness },
  { id: "inbox" as View, label: "Inbox", icon: MessageCircle },
  { id: "saved" as View, label: "Saved", icon: BookmarkIcon },
  { id: "updates" as View, label: "Updates", icon: Bell },
];
export default function App() {
  const [account, setAccount] = useState<Account | null>(null),
    [guestMode, setGuestMode] = useState<Mode>("Hiring"),
    [view, setView] = useState<View>("discover"),
    [dialog, setDialog] = useState<Dialog | null>(null),
    [revision, setRevision] = useState(0),
    [toast, setToast] = useState(""),
    [mobileNav, setMobileNav] = useState(false),
    [initialThread, setInitialThread] = useState<string | undefined>(),
    [restoring, setRestoring] = useState(hasToken());
  const config = useResource<Config>("/config", revision);
  const bookmarks = useResource<Bookmark[]>(
    account ? "/me/bookmarks" : null,
    revision,
  );
  const mode = account?.preferredMode || guestMode;
  const close = useCallback(() => setDialog(null), []);
  const refresh = () => setRevision((x) => x + 1);
  useEffect(() => {
    if (new URLSearchParams(location.search).has("mobileChallenge"))
      setDialog({ kind: "signin" });
    if (!hasToken()) return;
    api<Account>("/me/")
      .then(setAccount)
      .catch(() => setToken(null))
      .finally(() => setRestoring(false));
  }, []);
  useEffect(() => {
    const expire = () => {
      setAccount(null);
      setView("discover");
      setDialog({ kind: "signin" });
      setToast("Your session expired. Sign in again to continue.");
    };
    window.addEventListener("auth-expired", expire);
    return () => window.removeEventListener("auth-expired", expire);
  }, []);
  useEffect(() => {
    if (!toast) return;
    const id = setTimeout(() => setToast(""), 4500);
    return () => clearTimeout(id);
  }, [toast]);
  useEffect(() => {
    const changed = () => {
      const value = location.hash.slice(1) as View;
      if (nav.some((x) => x.id === value)) setView(value);
    };
    changed();
    window.addEventListener("hashchange", changed);
    return () => window.removeEventListener("hashchange", changed);
  }, []);
  function go(v: View) {
    if (!account && v !== "discover") {
      setDialog({ kind: "signin" });
      return;
    }
    location.hash = v;
    setView(v);
    setMobileNav(false);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }
  async function run(
    action: () => Promise<unknown>,
    message: string,
    closeDialog = true,
  ) {
    try {
      await action();
      if (closeDialog) close();
      refresh();
      setToast(message);
    } catch (e) {
      setToast((e as Error).message);
    }
  }
  async function changeMode(next: Mode) {
    if (next === mode) return;
    if (!account) {
      setGuestMode(next);
      return;
    }
    try {
      setAccount(await api<Account>("/me/mode", "PUT", { mode: next }));
      refresh();
      if (next === "Working" && !account.hasProfile)
        setDialog({ kind: "profileForm" });
    } catch (e) {
      setToast((e as Error).message);
    }
  }
  async function signedIn(a: Account) {
    const challenge = new URLSearchParams(location.search).get(
      "mobileChallenge",
    );
    if (challenge && /^[A-Za-z0-9_-]{43}$/.test(challenge)) {
      try {
        const grant = await api<{ code: string }>(
          "/auth/mobile/grant",
          "POST",
          { challenge },
        );
        location.assign(
          "getyourgigs://auth?code=" + encodeURIComponent(grant.code),
        );
        setToast("Sign-in complete. Return to the Get Your Gigs app.");
      } catch (e) {
        setToast((e as Error).message);
      }
      close();
      return;
    }
    setAccount(a);
    refresh();
    close();
    if (guestMode === "Working" && !a.hasProfile) {
      try {
        const updated = await api<Account>("/me/mode", "PUT", {
          mode: "Working",
        });
        setAccount(updated);
        setDialog({ kind: "profileForm" });
      } catch (e) {
        setToast((e as Error).message);
      }
    } else setToast("Welcome in, " + displayName(a.name).split(" ")[0] + ".");
  }
  async function save(type: "profile" | "gig", id: string) {
    if (!account) {
      setDialog({ kind: "signin" });
      return;
    }
    const saved = bookmarks.data?.find(
      (x) => x.targetType === type && x.targetId === id,
    );
    await run(
      () =>
        saved
          ? api("/me/bookmarks/" + saved.id, "DELETE")
          : api("/me/bookmarks", "POST", { targetType: type, targetId: id }),
      saved
        ? "Removed from your saved collection."
        : "Saved for a little later.",
      false,
    );
  }
  function post() {
    setDialog(account ? { kind: "post" } : { kind: "signin" });
  }
  function message(recipientId: string, name: string, gigId?: string) {
    setDialog(
      account
        ? { kind: "message", recipientId, name, gigId }
        : { kind: "signin" },
    );
  }
  function apply(gig: Gig) {
    if (!account) {
      setDialog({ kind: "signin" });
      return;
    }
    if (!account.hasProfile) {
      setDialog({ kind: "profileForm" });
      return;
    }
    setDialog({ kind: "apply", gig });
  }
  async function editProfile() {
    try {
      setDialog({
        kind: "profileForm",
        profile: await api<Profile | null>("/me/profile"),
      });
    } catch (e) {
      setToast((e as Error).message);
    }
  }
  const listing = {
    onProfile: (profile: Profile) => setDialog({ kind: "profile", profile }),
    onGig: (gig: Gig) => setDialog({ kind: "gig", gig }),
    onSave: save,
    bookmarks: bookmarks.data || [],
  };
  function renderDialog() {
    if (!dialog) return null;
    switch (dialog.kind) {
      case "signin":
        return (
          <SignIn
            config={
              config.data || {
                googleClientId: null,
                developmentEmail: false,
                categories,
              }
            }
            onSignedIn={signedIn}
          />
        );
      case "profileForm":
        return (
          <ProfileForm
            existing={dialog.profile}
            onPublished={() => {
              setAccount((current) =>
                current ? { ...current, hasProfile: true } : current,
              );
              refresh();
            }}
            onSaved={() => {
              close();
              refresh();
              setToast("Your talent profile is live.");
            }}
          />
        );
      case "post":
        return (
          <PostGig
            existing={dialog.gig}
            onSaved={() => {
              close();
              refresh();
              go("gigs");
              setToast(
                dialog.gig
                  ? "Your gig has been updated."
                  : "Your gig is live. Let the right people find you.",
              );
            }}
          />
        );
      case "profile":
        return (
          <TalentDetail
            profile={dialog.profile}
            saved={Boolean(
              bookmarks.data?.some(
                (b) =>
                  b.targetType === "profile" &&
                  b.targetId === dialog.profile.id,
              ),
            )}
            onSaved={() => save("profile", dialog.profile.id)}
            onMessage={() =>
              message(dialog.profile.ownerId, dialog.profile.name)
            }
          />
        );
      case "gig": {
        const g = dialog.gig;
        return (
          <GigDetail
            gig={g}
            own={g.ownerId === account?.id}
            onApply={() => apply(g)}
            onMessage={() => message(g.ownerId, g.ownerName, g.id)}
            onApplicants={() => setDialog({ kind: "applicants", gig: g })}
            onEdit={() => setDialog({ kind: "post", gig: g })}
            onComplete={() =>
              setDialog({
                kind: "confirm",
                title: "Did the gig take place?",
                text: "Confirm the booked talent actually completed this gig. This enables a genuine review and closes the waitlist.",
                action: async () => {
                  await api("/gigs/" + g.id + "/complete", "POST");
                },
              })
            }
            onCloseGig={() =>
              setDialog({
                kind: "confirm",
                title: "Close this gig?",
                text: "This stops new applications and notifies everyone who applied. You can post another gig whenever you’re ready.",
                action: async () => {
                  await api("/gigs/" + g.id + "/close", "POST");
                },
              })
            }
          />
        );
      }
      case "apply":
        return (
          <ApplyGig
            gig={dialog.gig}
            onSent={() => {
              refresh();
              setDialog({
                kind: "success",
                title: "Your next chapter is in motion.",
                text: "Your application was sent. Follow its progress in Gigs, and keep an eye on your inbox.",
              });
            }}
          />
        );
      case "message":
        return (
          <MessageForm
            recipientId={dialog.recipientId}
            recipientName={dialog.name}
            gigId={dialog.gigId}
            onSent={(thread: Thread) => {
              setInitialThread(thread.id);
              close();
              refresh();
              go("inbox");
              setToast("Message sent. Good things start with hello.");
            }}
          />
        );
      case "applicants":
        return (
          <Applicants
            gig={dialog.gig}
            onChanged={refresh}
            onProfile={async (id) => {
              try {
                setDialog({
                  kind: "profile",
                  profile: await api<Profile>("/profiles/" + id),
                });
              } catch (e) {
                setToast((e as Error).message);
              }
            }}
            onMessage={(a) => message(a.applicantId, a.applicantName, a.gigId)}
          />
        );
      case "account":
        return (
          <>
            <span className="eyebrow">ONE ACCOUNT. EVERY POSSIBILITY.</span>
            <h2>
              Hello,{" "}
              <em>{displayName(account?.name || "you").split(" ")[0]}.</em>
            </h2>
            <p>{account?.email}</p>
            <ActionForm
              submitLabel="Save name"
              onSubmit={async (f) => {
                setAccount(
                  await api<Account>("/me/", "PUT", { name: f.get("name") }),
                );
                setToast("Your name is updated.");
                close();
                refresh();
              }}
            >
              <Field label="Your display name">
                <input
                  name="name"
                  defaultValue={account?.name}
                  minLength={2}
                  maxLength={80}
                  required
                />
              </Field>
            </ActionForm>
            <div className="account-actions">
              <button className="btn outline full" onClick={editProfile}>
                <UserRound size={17} />
                {account?.hasProfile
                  ? "Edit your talent profile"
                  : "Create your talent profile"}
              </button>
              <button
                className="link danger"
                onClick={() => {
                  setToken(null);
                  setAccount(null);
                  close();
                  go("discover");
                  refresh();
                  setToast("You’ve signed out on this device.");
                }}
              >
                <LogOut size={15} />
                Sign out on this device
              </button>
            </div>
            <p className="fine-print">
              You’re currently {mode.toLowerCase()}. Switch anytime using the
              toggle above.
            </p>
          </>
        );
      case "success":
        return (
          <div className="success-screen">
            <CheckCircle2 className="success-symbol" size={44} />
            <h2>{dialog.title}</h2>
            <p>{dialog.text}</p>
            <button
              className="btn primary full"
              onClick={() => {
                close();
                go("gigs");
              }}
            >
              See my applications <ArrowRight size={17} />
            </button>
          </div>
        );
      case "confirm":
        return (
          <>
            <span className="eyebrow">ONE QUICK CHECK</span>
            <h2>{dialog.title}</h2>
            <p>{dialog.text}</p>
            <ActionForm
              submitLabel="Confirm"
              onSubmit={async () => {
                await dialog.action();
                close();
                refresh();
                setToast("All done. Your updates are saved.");
              }}
            />
            <button className="link centered" onClick={close}>
              Go back
            </button>
          </>
        );
    }
  }
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
      <header className="topbar">
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
                onClick={() => go(n.id)}
              >
                {n.label}
              </button>
            ))}
          </nav>
          <div className="header-actions">
            {account ? (
              <>
                <div className="context-toggle" aria-label="Account context">
                  {(["Hiring", "Working"] as Mode[]).map((m) => (
                    <button
                      key={m}
                      className={mode === m ? "selected" : ""}
                      aria-pressed={mode === m}
                      onClick={() => changeMode(m)}
                    >
                      {m}
                    </button>
                  ))}
                </div>
                <button
                  className="avatar-button"
                  aria-label="Your account"
                  onClick={() => setDialog({ kind: "account" })}
                >
                  <Avatar name={account.name} />
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
              aria-label="Toggle navigation"
              aria-expanded={mobileNav}
              onClick={() => setMobileNav(!mobileNav)}
            >
              {mobileNav ? <X size={21} /> : <Menu size={21} />}
            </button>
          </div>
        </div>
        {mobileNav && (
          <nav className="mobile-dropdown">
            {nav.map((n) => (
              <button key={n.id} onClick={() => go(n.id)}>
                <n.icon size={17} />
                {n.label}
              </button>
            ))}
          </nav>
        )}
      </header>
      <main className="container">
        {!account && (
          <div className="guest-context">
            <button
              className={mode === "Hiring" ? "selected" : ""}
              onClick={() => changeMode("Hiring")}
            >
              <BriefcaseBusiness size={14} />I want to hire
            </button>
            <button
              className={mode === "Working" ? "selected" : ""}
              onClick={() => changeMode("Working")}
            >
              <UserRound size={14} />I want to work
            </button>
          </div>
        )}
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
      <footer className="footer container">
        <Brand />
        <span>For people who make things happen.</span>
        <span className="footer-note">Built on real connections. Always.</span>
      </footer>
      {account && (
        <nav className="bottom-nav" aria-label="Mobile navigation">
          {nav.map((n) => (
            <button
              key={n.id}
              className={view === n.id ? "active" : ""}
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
          onClose={close}
          wide={["profile", "gig", "applicants"].includes(dialog.kind)}
        >
          {renderDialog()}
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
