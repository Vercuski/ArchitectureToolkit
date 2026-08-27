import { UserManager, WebStorageStateStore, type UserManagerSettings } from 'oidc-client-ts'

// ADR-0003/ADR-0005: in production, the SPA and API are genuinely
// same-origin (served from the API's own wwwroot), so authority is just
// window.location.origin — a deployer only needs to update the API's
// Authentication:RedirectUris/PostLogoutRedirectUris config to match its
// real origin (see ADR-0003's IdentityBootstrapper follow-up note), no
// code change here. In local dev, the SPA and API run as separate
// processes on separate ports, so VITE_API_BASE_URL (.env.development)
// points directly at the real API port; the backend's dev-only CORS
// policy (Program.cs) is what allows this cross-origin access, rather
// than trying to make dev look same-origin via a proxy — that approach
// was tried and abandoned: OpenIddict's self-hosted server resolves its
// discovery document's issuer and endpoint URIs from its own bound
// address, not from proxy-rewritten request headers, so a proxied dev
// origin still ends up bypassed by oidc-client-ts's own (correct, spec-
// compliant) handling of those absolute discovery URLs.
const authority = import.meta.env.VITE_API_BASE_URL || window.location.origin

const settings: UserManagerSettings = {
  authority,
  client_id: 'architecturetoolkit-spa',
  redirect_uri: `${window.location.origin}/auth/callback`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  response_type: 'code',
  scope: 'openid email offline_access architecturetoolkit-api',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: true,
}

export const userManager = new UserManager(settings)
