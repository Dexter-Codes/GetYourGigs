import { ArrowRight, CheckCircle2, LogOut, UserRound } from "lucide-react";
import { api, setToken } from "../api";

import { ActionForm, Field } from "../components";
import {
  Applicants,
  ApplyGig,
  GigDetail,
  MessageForm,
  PostGig,
  ProfileForm,
  SignIn,
  TalentDetail,
} from "../flows";

import {
  type Account,
  type Profile,
  type Thread,
  categories,
  displayName,
} from "../types";
import type { MarketplaceController } from "./useMarketplace";
export function AppDialog({
  marketplace,
}: {
  marketplace: MarketplaceController;
}) {
  const {
    account,
    setAccount,
    dialog,
    setDialog,
    setToast,
    setInitialThread,
    config,
    bookmarks,
    mode,
    close,
    refresh,
    go,
    signedIn,
    save,
    message,
    apply,
    editProfile,
  } = marketplace;
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
                b.targetType === "profile" && b.targetId === dialog.profile.id,
            ),
          )}
          onSaved={() => save("profile", dialog.profile.id)}
          onMessage={() => message(dialog.profile.ownerId, dialog.profile.name)}
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
            Hello, <em>{displayName(account?.name || "you").split(" ")[0]}.</em>
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
