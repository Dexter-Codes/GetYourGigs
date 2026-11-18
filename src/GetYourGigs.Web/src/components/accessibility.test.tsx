import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { Field } from "./Field";
import { ModeSwitcher } from "./ModeSwitcher";
import { CategoryTiles } from "./CategoryTiles";
import { Modal } from "./Modal";
import { categories } from "../types";

afterEach(cleanup);

it("gives fields a clear name and connects helper text", () => {
  render(
    <Field label="Your name" hint="Use the name you want others to see.">
      <input />
    </Field>,
  );
  const control = screen.getByRole("textbox", { name: "Your name" });
  const hintId = control.getAttribute("aria-describedby")!;
  expect(document.getElementById(hintId)?.textContent).toBe(
    "Use the name you want others to see.",
  );
});

it("exposes the selected marketplace view and lets people switch", () => {
  const change = vi.fn();
  const view = render(<ModeSwitcher mode="Hiring" onChange={change} />);
  expect(
    screen
      .getByRole("button", { name: "Hire talent" })
      .getAttribute("aria-pressed"),
  ).toBe("true");
  fireEvent.click(screen.getByRole("button", { name: "Find work" }));
  expect(change).toHaveBeenCalledWith("Working");
  view.rerender(<ModeSwitcher mode="Working" onChange={change} />);
  expect(
    screen
      .getByRole("button", { name: "Find work" })
      .getAttribute("aria-pressed"),
  ).toBe("true");
});

it("announces category selection and allows clearing it", () => {
  const select = vi.fn();
  render(<CategoryTiles selected={categories[0]} onSelect={select} />);
  const category = screen.getByRole("button", { name: categories[0] });
  expect(category.getAttribute("aria-pressed")).toBe("true");
  fireEvent.click(category);
  expect(select).toHaveBeenCalledWith("");
});

it("names dialogs and moves keyboard focus from the container to its first control", () => {
  const close = vi.fn();
  render(
    <Modal label="Apply to this gig" onClose={close}>
      <input aria-label="Application note" />
    </Modal>,
  );
  const dialog = screen.getByRole("dialog", { name: "Apply to this gig" });
  expect(document.activeElement).toBe(dialog);
  fireEvent.keyDown(document, { key: "Tab" });
  expect(document.activeElement).toBe(
    screen.getByRole("button", { name: "Close dialog" }),
  );
  fireEvent.keyDown(document, { key: "Escape" });
  expect(close).toHaveBeenCalledOnce();
});
