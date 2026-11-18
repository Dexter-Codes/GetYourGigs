import { useState } from "react";
import { Check, MapPin } from "lucide-react";
import { api } from "../api";

import { ActionForm, CategoryTiles, Field, PhotoInput } from "../components";
import { type Gig, type GigInput, dateLabel, money } from "../types";

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
