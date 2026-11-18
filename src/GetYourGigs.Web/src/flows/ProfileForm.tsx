import { useState } from "react";
import { ArrowRight, CheckCircle2 } from "lucide-react";
import { api } from "../api";

import { ActionForm, CategorySelect, Field, PhotoInput } from "../components";
import { type Profile, type ProfileInput } from "../types";

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
