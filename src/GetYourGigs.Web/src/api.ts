import { useEffect, useState } from "react";
let token = sessionStorage.getItem("gyg-token");
export const setToken = (value: string | null) => {
  token = value;
  if (value) sessionStorage.setItem("gyg-token", value);
  else sessionStorage.removeItem("gyg-token");
};
export const hasToken = () => !!token;
export async function api<T>(
  path: string,
  method = "GET",
  body?: unknown,
): Promise<T> {
  const res = await fetch("/api" + path, {
    method,
    headers: {
      ...(body instanceof FormData
        ? {}
        : { "Content-Type": "application/json" }),
      ...(token ? { Authorization: "Bearer " + token } : {}),
    },
    ...(body === undefined
      ? {}
      : { body: body instanceof FormData ? body : JSON.stringify(body) }),
  });
  if (!res.ok) {
    const p = await res.json().catch(() => ({}));
    if (res.status === 401 && token) {
      setToken(null);
      window.dispatchEvent(new Event("auth-expired"));
    }
    throw new Error(
      p.detail ||
        Object.values(p.errors || {})
          .flat()
          .join(" ") ||
        (res.status === 429
          ? "Too many requests. Please try again shortly."
          : "Unable to complete this request."),
    );
  }
  const text = await res.text();
  return (text ? JSON.parse(text) : null) as T;
}
export function useResource<T>(path: string | null, version = 0) {
  const requestKey = JSON.stringify([path, version]);
  const [loadedKey, setLoadedKey] = useState(requestKey);
  const [data, setData] = useState<T | null>(null),
    [loading, setLoading] = useState(true),
    [error, setError] = useState("");
  useEffect(() => {
    let active = true;
    setLoadedKey(requestKey);
    setData(null);
    setLoading(true);
    setError("");
    if (!path) {
      setData(null);
      setLoading(false);
      return;
    }
    api<T>(path)
      .then((x) => {
        if (active) setData(x);
      })
      .catch((e) => {
        if (active) setError(e.message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [path, version, requestKey]);
  const current = loadedKey === requestKey;
  return {
    data: current ? data : null,
    loading: !current || loading,
    error: current ? error : "",
    setData,
  };
}
export function useDebounce(value: string, ms = 300) {
  const [v, setV] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setV(value), ms);
    return () => clearTimeout(id);
  }, [value, ms]);
  return v;
}
