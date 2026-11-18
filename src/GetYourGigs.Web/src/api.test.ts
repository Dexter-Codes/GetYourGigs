import { afterEach, describe, expect, it, vi } from "vitest";
import { api, ApiError, hasToken, setToken } from "./api";

afterEach(() => {
  setToken(null);
  vi.unstubAllGlobals();
});

describe("HTTP error handling", () => {
  it.each([null, [], { detail: 42 }, { errors: { name: "invalid" } }])(
    "handles malformed Problem Details without masking the status: %j",
    async (body) => {
      vi.stubGlobal(
        "fetch",
        vi
          .fn()
          .mockResolvedValue(
            new Response(JSON.stringify(body), { status: 400 }),
          ),
      );
      await expect(api("/gigs")).rejects.toMatchObject({
        status: 400,
        message: "Unable to complete this request.",
      });
    },
  );

  it("keeps a newer login when an old request returns unauthorized", async () => {
    let complete!: (response: Response) => void;
    vi.stubGlobal(
      "fetch",
      vi.fn(
        () =>
          new Promise<Response>((resolve) => {
            complete = resolve;
          }),
      ),
    );
    setToken("old-session");
    const request = api("/me/").catch((error) => error);
    setToken("new-session");
    complete(new Response("{}", { status: 401 }));
    expect(await request).toBeInstanceOf(ApiError);
    expect(sessionStorage.getItem("gyg-token")).toBe("new-session");
    expect(hasToken()).toBe(true);
  });

  it("expires the current login even when the error body is not JSON", async () => {
    setToken("expired-session");
    const expired = vi.fn();
    window.addEventListener("auth-expired", expired);
    try {
      vi.stubGlobal(
        "fetch",
        vi
          .fn()
          .mockResolvedValue(
            new Response("<html>Unauthorized</html>", { status: 401 }),
          ),
      );
      await expect(api("/me/")).rejects.toMatchObject({ status: 401 });
      expect(hasToken()).toBe(false);
      expect(expired).toHaveBeenCalledOnce();
    } finally {
      window.removeEventListener("auth-expired", expired);
    }
  });
});
