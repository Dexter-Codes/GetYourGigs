import { LoaderCircle } from "lucide-react";

export function Loading() {
  return (
    <div className="loading" role="status" aria-live="polite">
      <LoaderCircle size={23} className="spin" aria-hidden="true" /> Loading…
    </div>
  );
}
