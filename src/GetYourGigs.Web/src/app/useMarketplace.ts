import { useCallback, useEffect, useRef, useState } from "react";
import { api, hasToken } from "../api";
import { useResource } from "../hooks/useResource";
import {
  type Account,
  type Bookmark,
  type Config,
  type Mode,
  type Profile,
  type Gig,
  displayName,
} from "../types";
import { nav, type View, type Dialog } from "./navigation";

export function useMarketplace() {
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
    const controller = new AbortController();
    api<Account>("/me/", "GET", undefined, controller.signal)
      .then((account) => {
        if (!controller.signal.aborted) setAccount(account);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setToast(error.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setRestoring(false);
      });
    return () => controller.abort();
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
  const modeChangePending = useRef(false);
  async function changeMode(next: Mode) {
    if (next === mode || modeChangePending.current) return;
    if (!account) {
      setGuestMode(next);
      return;
    }
    try {
      modeChangePending.current = true;
      setAccount(await api<Account>("/me/mode", "PUT", { mode: next }));
      refresh();
      if (next === "Working" && !account.hasProfile)
        setDialog({ kind: "profileForm" });
    } catch (e) {
      setToast((e as Error).message);
    } finally {
      modeChangePending.current = false;
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
  return {
    account,
    setAccount,
    view,
    dialog,
    setDialog,
    revision,
    toast,
    setToast,
    mobileNav,
    setMobileNav,
    initialThread,
    setInitialThread,
    restoring,
    config,
    bookmarks,
    mode,
    close,
    refresh,
    go,
    changeMode,
    signedIn,
    save,
    post,
    message,
    apply,
    editProfile,
    listing,
  };
}

export type MarketplaceController = ReturnType<typeof useMarketplace>;
