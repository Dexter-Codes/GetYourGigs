let token = sessionStorage.getItem("gyg-token");
let sessionVersion = 0;

export function setToken(value: string | null) {
  token = value;
  sessionVersion++;
  if (value) sessionStorage.setItem("gyg-token", value);
  else sessionStorage.removeItem("gyg-token");
}

export const hasToken = () => Boolean(token);

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function problemMessage(problem: unknown, status: number): string {
  if (isObject(problem)) {
    if (typeof problem.detail === "string" && problem.detail.trim())
      return problem.detail;
    if (isObject(problem.errors)) {
      const messages = Object.values(problem.errors)
        .filter(Array.isArray)
        .flat()
        .filter(
          (value): value is string =>
            typeof value === "string" && Boolean(value.trim()),
        );
      if (messages.length) return messages.join(" ");
    }
  }
  return status === 429
    ? "Too many requests. Please try again shortly."
    : "Unable to complete this request.";
}

/** HTTP transport only. Marketplace policy and authorization remain in the API. */
export async function api<T>(
  path: string,
  method = "GET",
  body?: unknown,
  signal?: AbortSignal,
): Promise<T> {
  const requestToken = token;
  const requestSession = sessionVersion;
  const response = await fetch("/api" + path, {
    method,
    signal,
    headers: {
      ...(body instanceof FormData
        ? {}
        : { "Content-Type": "application/json" }),
      ...(requestToken ? { Authorization: "Bearer " + requestToken } : {}),
    },
    ...(body === undefined
      ? {}
      : { body: body instanceof FormData ? body : JSON.stringify(body) }),
  });
  if (!response.ok) {
    // An obsolete request must not sign out a subsequently authenticated account.
    if (
      response.status === 401 &&
      requestToken &&
      requestSession === sessionVersion
    ) {
      setToken(null);
      window.dispatchEvent(new Event("auth-expired"));
    }
    const problem: unknown = await response.json().catch(() => null);
    throw new ApiError(
      response.status,
      problemMessage(problem, response.status),
    );
  }
  const text = await response.text();
  return (text ? JSON.parse(text) : null) as T;
}
