import {
  Music2,
  Wrench,
  Palette,
  Laptop,
  CalendarDays,
  HeartPulse,
  BriefcaseBusiness,
  Shapes,
} from "lucide-react";
import { categories } from "../types";

const icons = [
  Music2,
  Wrench,
  Palette,
  Laptop,
  CalendarDays,
  HeartPulse,
  BriefcaseBusiness,
  Shapes,
];

export function CategoryTiles({
  selected,
  onSelect,
}: {
  selected: string;
  onSelect: (value: string) => void;
}) {
  return (
    <div
      className="category-tiles"
      role="group"
      aria-label="Filter by category"
    >
      {categories.map((category, index) => {
        const Icon = icons[index] ?? Shapes;
        return (
          <button
            key={category}
            type="button"
            className={`category-tile tile-${index}${selected === category ? " selected" : ""}`}
            aria-pressed={selected === category}
            onClick={() => onSelect(selected === category ? "" : category)}
          >
            <span>
              <Icon size={24} aria-hidden="true" />
            </span>
            <small>{category}</small>
          </button>
        );
      })}
    </div>
  );
}
