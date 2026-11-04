export function createIdempotencyKey(): string {
  if (typeof globalThis.crypto?.getRandomValues === "function") {
    const values = new Uint32Array(4);
    globalThis.crypto.getRandomValues(values);

    const suffix = Array.from(values, (value) =>
      value.toString(16).padStart(8, "0"),
    ).join("");

    return `web-${suffix}`;
  }

  const timestamp = Date.now().toString(36);
  const random = Math.random().toString(36).slice(2);

  return `web-${timestamp}-${random}`;
}
