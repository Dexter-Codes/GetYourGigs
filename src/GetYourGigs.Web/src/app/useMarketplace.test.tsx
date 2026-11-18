import { cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { hasToken, setToken } from "../api";
import { useMarketplace } from "./useMarketplace";

afterEach(() => {
  cleanup();
  setToken(null);
  vi.unstubAllGlobals();
});

it("does not erase the saved login after a temporary restore failure", async () => {
  setToken("valid-session");
  vi.stubGlobal(
    "fetch",
    vi.fn((path: string) =>
      Promise.resolve(
        path === "/api/me/"
          ? new Response("Unavailable", { status: 503 })
          : new Response(
              JSON.stringify({
                categories: [],
                googleClientId: null,
                developmentEmail: false,
              }),
            ),
      ),
    ),
  );
  const { result } = renderHook(() => useMarketplace());
  await waitFor(() => expect(result.current.restoring).toBe(false));
  expect(hasToken()).toBe(true);
  expect(result.current.account).toBeNull();
  expect(result.current.toast).toBe("Unable to complete this request.");
});
