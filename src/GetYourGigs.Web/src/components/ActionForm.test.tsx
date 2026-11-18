import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { ActionForm } from "./ActionForm";

afterEach(cleanup);

it("submits a mutation only once while a previous submission is pending", async () => {
  let finish!: () => void;
  const submit = vi.fn(
    () =>
      new Promise<void>((resolve) => {
        finish = resolve;
      }),
  );
  render(<ActionForm onSubmit={submit} submitLabel="Book talent" />);
  const form = screen
    .getByRole("button", { name: "Book talent" })
    .closest("form")!;
  fireEvent.submit(form);
  fireEvent.submit(form);
  expect(submit).toHaveBeenCalledOnce();
  await act(async () => finish());
  expect(screen.getByRole("button", { name: "Book talent" })).toBeTruthy();
});
