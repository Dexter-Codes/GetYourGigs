import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { useInbox } from "./useInbox";

const response = (value: unknown) =>
  new Response(JSON.stringify(value), { status: 200 });
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

it("does not clear another conversation's draft when a send completes", async () => {
  let completeSend!: (response: Response) => void;
  const fetchMock = vi.fn((_path: string, options?: RequestInit) =>
    options?.method === "POST"
      ? new Promise<Response>((resolve) => {
          completeSend = resolve;
        })
      : Promise.resolve(response([])),
  );
  vi.stubGlobal("fetch", fetchMock);
  const { result } = renderHook(() => useInbox("first", 0));
  act(() => result.current.setText("First reply"));
  let sending!: Promise<void>;
  act(() => {
    sending = result.current.send();
  });
  act(() => result.current.setSelected("second"));
  act(() => result.current.setText("Keep this second draft"));
  await act(async () => {
    completeSend(response({}));
    await sending;
  });
  expect(result.current.text).toBe("Keep this second draft");
  const sent = fetchMock.mock.calls.find(
    ([, options]) => options?.method === "POST",
  );
  expect(sent?.[0]).toBe("/api/threads/first/messages");
  act(() => result.current.setSelected("first"));
  expect(result.current.text).toBe("");
});

it("preserves edits made during a send and prevents duplicate submissions", async () => {
  let completeSend!: (response: Response) => void;
  const fetchMock = vi.fn((_path: string, options?: RequestInit) =>
    options?.method === "POST"
      ? new Promise<Response>((resolve) => {
          completeSend = resolve;
        })
      : Promise.resolve(response([])),
  );
  vi.stubGlobal("fetch", fetchMock);
  const { result } = renderHook(() => useInbox("first", 0));
  act(() => result.current.setText("Original reply"));
  let sending!: Promise<void>;
  act(() => {
    sending = result.current.send();
    void result.current.send();
  });
  act(() => result.current.setText("New draft while sending"));
  await act(async () => {
    completeSend(response({}));
    await sending;
  });
  expect(result.current.text).toBe("New draft while sending");
  expect(
    fetchMock.mock.calls.filter(([, options]) => options?.method === "POST"),
  ).toHaveLength(1);
  await waitFor(() => expect(result.current.sending).toBe(false));
});
