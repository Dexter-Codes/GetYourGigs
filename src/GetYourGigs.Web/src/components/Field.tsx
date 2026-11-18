import { cloneElement, isValidElement, useId, type ReactNode } from "react";

type FieldControl = {
  id?: string;
  "aria-describedby"?: string;
  "aria-labelledby"?: string;
};

export function Field({
  label,
  children,
  hint,
}: {
  label: string;
  children: ReactNode;
  hint?: string;
}) {
  const fieldId = useId();
  const labelId = `${fieldId}-label`;
  const hintId = `${fieldId}-hint`;
  const isControl =
    isValidElement<FieldControl>(children) &&
    typeof children.type === "string" &&
    ["input", "select", "textarea"].includes(children.type);
  const controlId = isControl ? (children.props.id ?? fieldId) : undefined;
  const control = isControl
    ? cloneElement(children, {
        id: controlId,
        "aria-labelledby": children.props["aria-labelledby"] ?? labelId,
        "aria-describedby":
          [children.props["aria-describedby"], hint ? hintId : undefined]
            .filter(Boolean)
            .join(" ") || undefined,
      })
    : children;
  return (
    <label className="field" htmlFor={controlId}>
      <span id={labelId}>{label}</span>
      {control}
      {hint && <small id={hintId}>{hint}</small>}
    </label>
  );
}
