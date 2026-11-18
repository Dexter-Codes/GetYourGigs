import { AlertCircle } from "lucide-react";

export function ErrorBox({ message }: { message: string }) {
  return message ? (
    <div role="alert" className="error-box">
      <AlertCircle size={17} />
      {message}
    </div>
  ) : null;
}
