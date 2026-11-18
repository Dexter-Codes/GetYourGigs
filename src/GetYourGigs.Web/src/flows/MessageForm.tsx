import { api } from "../api";

import { ActionForm, Field } from "../components";
import { type Thread, displayName } from "../types";

export function MessageForm({
  recipientId,
  recipientName,
  gigId,
  onSent,
}: {
  recipientId: string;
  recipientName: string;
  gigId?: string;
  onSent: (thread: Thread) => void;
}) {
  return (
    <>
      <span className="eyebrow">START SOMETHING GOOD</span>
      <h2>
        Say hello to
        <br />
        <em>{displayName(recipientName)}.</em>
      </h2>
      <p>
        Ask a question, share your brief, or discuss availability. They can
        reply whenever they’re free.
      </p>
      <ActionForm
        submitLabel="Send message"
        onSubmit={async (f) =>
          onSent(
            await api<Thread>("/threads", "POST", {
              recipientId,
              gigId: gigId || null,
              message: f.get("message"),
            }),
          )
        }
      >
        <Field label="Your message">
          <textarea
            required
            minLength={1}
            maxLength={3000}
            name="message"
            placeholder="Hi! I’d love to talk about…"
          />
        </Field>
      </ActionForm>
    </>
  );
}
