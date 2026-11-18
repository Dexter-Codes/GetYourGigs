import { useState } from "react";
import { X, Upload } from "lucide-react";
import { api } from "../api";

import { ErrorBox } from "./ErrorBox";

export function PhotoInput({
  photos,
  onChange,
}: {
  photos: string[];
  onChange: (v: string[]) => void;
}) {
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  return (
    <div className="field">
      <span>
        Photos <small>optional · up to 5</small>
      </span>
      <div className="photo-input">
        {photos.map((url, i) => (
          <div className="photo-thumb" key={url}>
            <img src={url} alt={"Photo " + (i + 1)} />
            <button
              type="button"
              aria-label={`Remove photo ${i + 1}`}
              disabled={busy}
              onClick={() => onChange(photos.filter((_, j) => j !== i))}
            >
              <X size={12} />
            </button>
          </div>
        ))}
        {photos.length < 5 && (
          <label className="upload-tile">
            <Upload size={19} />
            <small>{busy ? "Uploading…" : "Add a photo"}</small>
            <input
              type="file"
              aria-label="Add a photo (JPEG, PNG or WebP, up to 5 MB)"
              accept="image/jpeg,image/png,image/webp"
              disabled={busy}
              onChange={async (e) => {
                const input = e.currentTarget;
                const file = input.files?.[0];
                if (!file) return;
                setBusy(true);
                setError("");
                try {
                  const body = new FormData();
                  body.append("file", file);
                  const result = await api<{ url: string }>(
                    "/uploads",
                    "POST",
                    body,
                  );
                  onChange([...photos, result.url]);
                } catch (e) {
                  setError((e as Error).message);
                } finally {
                  input.value = "";
                  setBusy(false);
                }
              }}
            />
          </label>
        )}
      </div>
      <small className="photo-help">
        JPEG, PNG or WebP · up to 5 MB per photo
      </small>
      <ErrorBox message={error} />
    </div>
  );
}
