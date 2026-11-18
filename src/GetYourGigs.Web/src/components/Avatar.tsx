import { initials } from "../types";

export function Avatar({ name, photo }: { name: string; photo?: string }) {
  return photo ? (
    <img className="avatar" src={photo} alt="" />
  ) : (
    <span className="avatar">{initials(name)}</span>
  );
}
