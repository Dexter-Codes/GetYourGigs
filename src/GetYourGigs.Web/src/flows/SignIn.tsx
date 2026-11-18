import { useEffect, useRef, useState } from "react";
import { Mail } from "lucide-react";
import { api, setToken } from "../api";

import { ActionForm, Brand, ErrorBox, Field } from "../components";
import { type Account, type Auth, type Config } from "../types";

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
        width: Math.max(200, Math.min(360, googleRef.current.clientWidth)),
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
      <span className="eyebrow">WELCOME TO GET YOUR GIGS</span>
      <h2>
        Sign in or <em>create an account.</em>
      </h2>
      <p>
        Use your email to get a sign-in code. No password needed. Your account
        lets you hire and find work.
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
            submitLabel="Verify code and sign in"
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
                placeholder="The name you’d like to use"
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
        New here? Verifying your email creates your account. You can hire and
        find work with the same account.
      </p>
    </>
  );
}
