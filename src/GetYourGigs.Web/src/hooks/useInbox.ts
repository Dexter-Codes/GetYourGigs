import { useEffect, useRef, useState } from "react";
import { api } from "../api";
import type { Message, Thread } from "../types";
import { useResource } from "./useResource";

/** Owns conversation selection, refresh timing, and per-conversation drafts. */
export function useInbox(initialThread: string | undefined, revision: number) {
  const [selected, setSelected] = useState(initialThread || "");
  const [tick, setTick] = useState(0);
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const sendInFlight = useRef(false);
  const threads = useResource<Thread[]>("/threads", revision + tick);
  const messages = useResource<Message[]>(
    selected ? `/threads/${selected}/messages` : null,
    revision + tick,
  );

  useEffect(() => {
    const timer = setInterval(() => {
      if (!document.hidden) setTick((value) => value + 1);
    }, 10_000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (initialThread) setSelected(initialThread);
  }, [initialThread]);

  useEffect(() => {
    if (!selected && threads.data?.length) setSelected(threads.data[0].id);
  }, [threads.data, selected]);

  const text = drafts[selected] ?? "";
  function setText(value: string) {
    setDrafts((current) => ({ ...current, [selected]: value }));
  }

  async function send() {
    if (!selected || !text.trim() || sendInFlight.current) return;
    const threadId = selected;
    const submittedText = text;
    sendInFlight.current = true;
    setSending(true);
    setError("");
    try {
      await api(`/threads/${threadId}/messages`, "POST", {
        text: submittedText,
      });
      setDrafts((current) => {
        // Preserve a newer draft, even if the user stayed in the same thread.
        if (current[threadId] !== submittedText) return current;
        return { ...current, [threadId]: "" };
      });
      setTick((value) => value + 1);
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : "Your reply could not be sent.",
      );
    } finally {
      sendInFlight.current = false;
      setSending(false);
    }
  }

  return {
    selected,
    setSelected,
    text,
    setText,
    sending,
    error,
    threads,
    messages,
    send,
    current: threads.data?.find((thread) => thread.id === selected),
    refresh: () => setTick((value) => value + 1),
  };
}
