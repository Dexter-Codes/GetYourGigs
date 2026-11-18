import { categories } from "../types";

export function CategorySelect({
  value,
  onChange,
  name = "category",
}: {
  value?: string;
  onChange?: (value: string) => void;
  name?: string;
}) {
  return (
    <select
      name={name}
      value={value}
      onChange={(e) => onChange?.(e.target.value)}
      required
    >
      <option value="">Choose a category</option>
      {categories.map((x) => (
        <option key={x}>{x}</option>
      ))}
    </select>
  );
}
