import { BriefcaseBusiness, Search } from "lucide-react";
import type { Mode } from "../types";

export function ModeSwitcher({
  mode,
  onChange,
}: {
  mode: Mode;
  onChange: (mode: Mode) => void;
}) {
  return (
    <section className="mode-switcher" aria-label="Choose what you want to do">
      <div>
        <p className="mode-label">What would you like to do?</p>
        <p className="mode-hint">
          One account for hiring and working. Switch whenever you need.
        </p>
      </div>
      <div className="mode-options" role="group" aria-label="Marketplace view">
        <button
          type="button"
          aria-pressed={mode === "Hiring"}
          onClick={() => onChange("Hiring")}
        >
          <BriefcaseBusiness size={19} aria-hidden="true" /> Hire talent
        </button>
        <button
          type="button"
          aria-pressed={mode === "Working"}
          onClick={() => onChange("Working")}
        >
          <Search size={19} aria-hidden="true" /> Find work
        </button>
      </div>
    </section>
  );
}
