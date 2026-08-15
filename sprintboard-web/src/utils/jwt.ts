interface JwtPayload {
  exp?: number;
}

function parseJwt(token: string): JwtPayload | null {
  try {
    const base64Url = token.split(".")[1];

    if (!base64Url) {
      return null;
    }

    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((character) =>
          `%${(`00${character.charCodeAt(0).toString(16)}`).slice(-2)}`
        )
        .join("")
    );

    return JSON.parse(jsonPayload) as JwtPayload;
  } catch {
    return null;
  }
}

export function isTokenExpired(token: string): boolean {
  const payload = parseJwt(token);

  if (!payload?.exp) {
    return true;
  }

  const currentTimeInSeconds = Math.floor(Date.now() / 1000);
  return payload.exp < currentTimeInSeconds;
}
