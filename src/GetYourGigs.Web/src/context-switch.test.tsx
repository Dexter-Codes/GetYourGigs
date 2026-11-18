import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useResource } from "./hooks/useResource";
import { Saved } from "./pages";
import type { Profile, Gig, Bookmark } from "./types";
const response = (data: unknown) =>
  new Response(JSON.stringify(data), { status: 200 });
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});
function DiscoveryProbe({ path }: { path: string }) {
  const { data, loading } = useResource<{
    items: { name?: string; title?: string }[];
  }>(path);
  if (loading || !data) return <p>Loading</p>;
  // Each context intentionally reads a different contract, as the real cards do.
  return (
    <p>
      {(path === "/profiles"
        ? data.items[0].name!
        : data.items[0].title!
      ).toUpperCase()}
    </p>
  );
}
describe("switching marketplace context", () => {
  it("does not render a talent response as a gig while the next request is pending", async () => {
    let resolveGigs!: (value: Response) => void;
    const gigs = new Promise<Response>((resolve) => {
      resolveGigs = resolve;
    });
    vi.stubGlobal(
      "fetch",
      vi.fn((input: string) =>
        input.includes("profiles")
          ? Promise.resolve(response({ items: [{ name: "A great person" }] }))
          : gigs,
      ),
    );
    const view = render(<DiscoveryProbe path="/profiles" />);
    await screen.findByText("A GREAT PERSON");
    view.rerender(<DiscoveryProbe path="/gigs" />);
    expect(screen.queryByText("A GREAT PERSON")).toBeNull();
    expect(screen.getByText("Loading")).toBeTruthy();
    await act(async () => {
      resolveGigs(response({ items: [{ title: "A great gig" }] }));
    });
    await screen.findByText("A GREAT GIG");
  });
  it("ignores a late response from the previous context", async () => {
    let resolveProfiles!: (value: Response) => void;
    const profiles = new Promise<Response>((resolve) => {
      resolveProfiles = resolve;
    });
    vi.stubGlobal(
      "fetch",
      vi.fn((input: string) =>
        input.includes("profiles")
          ? profiles
          : Promise.resolve(response({ items: [{ title: "Current gig" }] })),
      ),
    );
    const view = render(<DiscoveryProbe path="/profiles" />);
    view.rerender(<DiscoveryProbe path="/gigs" />);
    await screen.findByText("CURRENT GIG");
    await act(async () => {
      resolveProfiles(response({ items: [{ name: "Old person" }] }));
    });
    expect(screen.getByText("CURRENT GIG")).toBeTruthy();
    expect(screen.queryByText("OLD PERSON")).toBeNull();
  });
  it("reshapes Saved from talent cards to gig cards without reading the old item shape", async () => {
    const profile: Profile = {
      id: "talent",
      ownerId: "owner",
      name: "Saved person",
      headline: "Photographer",
      category: "Creative & Design",
      city: "Mumbai",
      rate: 5000,
      rateUnit: "per gig",
      bio: "A sample test profile",
      skills: [],
      photos: [],
      available: true,
      rating: null,
      reviewCount: 0,
    };
    const gig: Gig = {
      id: "job",
      ownerId: "hirer",
      ownerName: "Hirer",
      title: "Saved opportunity",
      category: "Creative & Design",
      city: "Mumbai",
      description: "A sample test opportunity",
      date: null,
      budget: 5000,
      status: "Open",
      photos: [],
      applicationCount: 0,
      createdAt: new Date().toISOString(),
    };
    const bookmarks: Bookmark[] = [
      { id: "b1", targetType: "profile", targetId: "talent" },
      { id: "b2", targetType: "gig", targetId: "job" },
    ];
    vi.stubGlobal(
      "fetch",
      vi.fn((input: string) =>
        Promise.resolve(response(input.includes("profiles") ? profile : gig)),
      ),
    );
    const actions = {
      bookmarks,
      revision: 0,
      onProfile: vi.fn(),
      onGig: vi.fn(),
      onSave: vi.fn(),
    };
    const view = render(<Saved mode="Hiring" {...actions} />);
    await screen.findByRole("button", { name: "Saved person" });
    view.rerender(<Saved mode="Working" {...actions} />);
    await screen.findByRole("button", { name: "Saved opportunity" });
    await waitFor(() =>
      expect(screen.queryByRole("button", { name: "Saved person" })).toBeNull(),
    );
  });
});
