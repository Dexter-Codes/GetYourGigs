import { cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { useResource } from "./useResource";

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

it("aborts obsolete reads on navigation and the current read on unmount", async () => {
  const signals: AbortSignal[] = [];
  vi.stubGlobal(
    "fetch",
    vi.fn((_path: string, options: RequestInit) => {
      const signal = options.signal!;
      signals.push(signal);
      return new Promise<Response>((_resolve, reject) => {
        signal.addEventListener("abort", () =>
          reject(new DOMException("Aborted", "AbortError")),
        );
      });
    }),
  );
  const view = renderHook(({ path }) => useResource(path), {
    initialProps: { path: "/profiles" },
  });
  view.rerender({ path: "/gigs" });
  expect(signals[0].aborted).toBe(true);
  await waitFor(() => expect(view.result.current.error).toBe(""));
  view.unmount();
  expect(signals[1].aborted).toBe(true);
});
