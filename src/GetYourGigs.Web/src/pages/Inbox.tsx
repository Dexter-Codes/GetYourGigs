import { useInbox } from "../hooks/useInbox";

import { RefreshCw, Send } from "lucide-react";

import { Avatar, Empty, ErrorBox, Loading } from "../components";
import { type Account, displayName } from "../types";

export function Inbox({
  user,
  initialThread,
  revision,
}: {
  user: Account;
  initialThread?: string;
  revision: number;
}) {
  const {
    selected,
    setSelected,
    text,
    setText,
    sending,
    error,
    threads,
    messages,
    current,
    send,
    refresh,
  } = useInbox(initialThread, revision);
  return (
    <section className="page">
      <div className="section-head">
        <div>
          <span className="eyebrow">GOOD WORK STARTS WITH A CONVERSATION</span>
          <h1>Your inbox.</h1>
          <p>
            Leave a message. Pick up where you left off. No need to be online
            together.
          </p>
        </div>
        <button className="btn outline small" onClick={refresh}>
          <RefreshCw size={14} />
          Refresh
        </button>
      </div>
      <ErrorBox message={threads.error || messages.error || error} />
      {threads.loading && !threads.data ? (
        <Loading />
      ) : !threads.data?.length ? (
        <Empty title="Your next connection starts with hello.">
          Message someone from their profile or a gig to start a conversation.
        </Empty>
      ) : (
        <div className="inbox">
          <aside className="thread-list">
            {threads.data.map((t) => (
              <button
                key={t.id}
                className={"thread-item " + (selected === t.id ? "active" : "")}
                onClick={() => {
                  setSelected(t.id);
                }}
              >
                <Avatar name={t.otherName} />
                <div>
                  <strong>{displayName(t.otherName)}</strong>
                  <small>{t.lastMessage}</small>
                </div>
                {t.unreadCount > 0 && (
                  <span className="unread-count">{t.unreadCount}</span>
                )}
              </button>
            ))}
          </aside>
          <div className="chat">
            <div className="chat-header">
              <Avatar name={current?.otherName || "Conversation"} />
              <div>
                <strong>
                  {displayName(current?.otherName || "Conversation")}
                </strong>
                <small>
                  {current?.gigTitle
                    ? displayName(current.gigTitle)
                    : "A new connection"}
                </small>
              </div>
              <span className="async-label">Reply when it suits you</span>
            </div>
            <div className="chat-messages">
              {messages.loading && !messages.data ? (
                <Loading />
              ) : (
                messages.data?.map((m) => (
                  <div
                    key={m.id}
                    className={
                      "message " + (m.senderId === user.id ? "mine" : "")
                    }
                  >
                    <p>{m.text}</p>
                    <time>
                      {new Date(m.createdAt).toLocaleString("en-IN", {
                        day: "numeric",
                        month: "short",
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </time>
                  </div>
                ))
              )}
            </div>
            <form
              className="chat-compose"
              onSubmit={async (e) => {
                e.preventDefault();
                await send();
              }}
            >
              <input
                required
                maxLength={3000}
                aria-label="Your reply"
                value={text}
                onChange={(e) => setText(e.target.value)}
                placeholder="Write your message…"
              />
              <button
                className="btn primary"
                disabled={sending || !text.trim()}
                aria-label="Send reply"
              >
                <Send size={17} aria-hidden="true" />
                <span className="sr-only">Send reply</span>
              </button>
            </form>
          </div>
        </div>
      )}
    </section>
  );
}
