import { ArrowUpRight, LockKeyhole } from "lucide-react";

export function PreviewGate({ onSignIn }: { onSignIn: () => void }) {
  return (
    <div className="gate">
      <div className="ghost-cards" aria-hidden="true">
        {[0, 1, 2].map((x) => (
          <div key={x}>
            <div />
            <h3>Your next opportunity</h3>
            <p>Good people. Great work.</p>
          </div>
        ))}
      </div>
      <div className="gate-content">
        <span className="lock">
          <LockKeyhole size={18} />
        </span>
        <h3>Sign in to see all listings</h3>
        <p>
          You’re viewing a preview. Sign in to see all listings, save favorites,
          and send messages.
        </p>
        <button className="btn primary" onClick={onSignIn}>
          Sign in or create an account <ArrowUpRight size={16} />
        </button>
        <small>One account for hiring and finding work.</small>
      </div>
    </div>
  );
}
