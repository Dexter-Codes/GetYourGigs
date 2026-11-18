import { useRef, useState, type FormEvent, type ReactNode } from "react";
import { LoaderCircle } from "lucide-react";

import { ErrorBox } from "./ErrorBox";

export function ActionForm({
  onSubmit,
  children,
  submitLabel = "Save",
  className = "",
}: {
  onSubmit: (form: FormData) => Promise<void>;
  children?: ReactNode;
  submitLabel?: string;
  className?: string;
}) {
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const submitting = useRef(false);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (submitting.current) return;
    submitting.current = true;
    const form = new FormData(e.currentTarget);
    setBusy(true);
    setError("");
    try {
      await onSubmit(form);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      submitting.current = false;
      setBusy(false);
    }
  }
  return (
    <form className={className} onSubmit={submit}>
      {children}
      <ErrorBox message={error} />
      <button className="btn primary full" disabled={busy}>
        {busy ? <LoaderCircle className="spin" size={17} /> : null}
        {busy ? "One moment…" : submitLabel}
      </button>
    </form>
  );
}
