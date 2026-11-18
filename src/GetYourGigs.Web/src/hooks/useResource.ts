import { useEffect, useState } from "react";
import { api } from "../api";

export function useResource<T>(path: string | null, version = 0) {
  const requestKey = JSON.stringify([path, version]);
  const [loadedKey, setLoadedKey] = useState(requestKey);
  const [data, setData] = useState<T | null>(null),
    [loading, setLoading] = useState(true),
    [error, setError] = useState("");
  useEffect(() => {
    let active = true;
    const controller = new AbortController();
    setLoadedKey(requestKey);
    setData(null);
    setLoading(true);
    setError("");
    if (!path) {
      setData(null);
      setLoading(false);
      return;
    }
    api<T>(path, "GET", undefined, controller.signal)
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
      controller.abort();
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
