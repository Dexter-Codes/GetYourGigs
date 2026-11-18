import { type ReactNode } from "react";

export function Empty({
  title,
  children,
  action,
}: {
  title: string;
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="empty">
      <span className="empty-art">✳</span>
      <h3>{title}</h3>
      <p>{children}</p>
      {action}
    </div>
  );
}
