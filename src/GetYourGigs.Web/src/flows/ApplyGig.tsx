import { api } from "../api";
import { useResource } from "../hooks/useResource";
import { ActionForm, Field } from "../components";
import { type Gig, displayName } from "../types";

export function ApplyGig({ gig, onSent }: { gig: Gig; onSent: () => void }) {
  const allowance = useResource<{ used: number; limit: number }>(
    "/me/allowance",
  );
  return (
    <>
      <span className="eyebrow">GIVE IT YOUR BEST SHOT</span>
      <h2>
        Your next gig
        <br />
        could be <em>this one.</em>
      </h2>
      <p>
        {displayName(gig.title)} · {gig.city}
      </p>
      <div className="notice">
        {allowance.data
          ? `${allowance.data.limit - allowance.data.used} of 3 applications left today`
          : "Up to 3 thoughtful applications per day."}
      </div>
      <ActionForm
        submitLabel="Send my application"
        onSubmit={async (f) => {
          await api("/gigs/" + gig.id + "/applications", "POST", {
            message: f.get("message"),
            expectedPrice: f.get("price") ? Number(f.get("price")) : null,
          });
          onSent();
        }}
      >
        <Field label="Why are you a great fit?">
          <textarea
            name="message"
            required
            minLength={20}
            maxLength={2000}
            placeholder="A little about your experience, availability, and what you’d bring to this gig…"
          />
        </Field>
        <Field label="Your quote (₹, optional)">
          <input
            name="price"
            type="number"
            min={0}
            max={10000000}
            placeholder="Leave blank to discuss"
          />
        </Field>
      </ActionForm>
    </>
  );
}
