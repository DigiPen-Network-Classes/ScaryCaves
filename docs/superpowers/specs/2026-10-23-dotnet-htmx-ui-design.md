# Scary Caves: replace the Next.js UI with Razor Components + htmx

Branch: `feature/new-ui`

## Why

`scarycaves-next` is a ~900-line Next.js/React app whose dependencies need fixing constantly
(see the stream of dependabot merges). The game itself lives in `ScaryCavesWeb` (ASP.NET Core,
SignalR, Orleans, Redis). The goal is to retire the npm toolchain **entirely** and serve the UI
from `ScaryCavesWeb` using only .NET, following the pattern of
`~/prj/EliteDangerous/EliteEvents/EliteEvents.Web`: static SSR Razor Components, htmx for
dynamic behaviour, htmx's SSE extension for server push, and vendored static assets under
`wwwroot/lib` (no CDN, no package manager).

Students do not consume the SignalR hub or the JSON endpoints, and no lecture material depends
on them, so the server's wire formats may change freely.

## Goals

- One deployable: `ScaryCavesWeb` serves every page, fragment, and the live stream.
- Zero npm: no `package.json`, no Node in any Dockerfile, no JS build step. The only JS on the
  site is vendored htmx (+ SSE extension), Google's reCAPTCHA script tag, and at most a small
  hand-written script for the cave page (like EliteEvents' `ticker.js`).
- Feature parity with the current UI (list below), same look (Bootstrap, background image,
  navbar, footer).
- Upgrade to .NET 10 (web and test projects, Dockerfile) along with compatible Orleans packages.

## Non-goals

- New game features.
- Multi-instance / scale-out. Today there is one replica, a localhost Orleans cluster, and no
  SignalR backplane; the new push mechanism may likewise be in-process.
- Keeping the JSON `/Home/*` API, the CORS policy, or `GameHub` for compatibility.

## Anti-goals (would make this a failure even if it works)

- Reintroducing a JS package manager or build step in any form (including a "just for
  bootstrap" `libman`/npm). Vendored files are copied in by hand and committed.
- Blazor interactive render modes (`blazor.web.js`, circuits). Static SSR only.
- Loading scripts or styles from a CDN (reCAPTCHA's own script is the one unavoidable exception).

## Pages and behaviour (parity checklist)

Current Next routes → new server routes. Exact URLs are the builder's call; these are suggestions.

| Today (Next)      | New                                                                                       |
|-------------------|-------------------------------------------------------------------------------------------|
| `/`               | Home: "Before you lies the entrance to …" with a link to the cave.                         |
| `/login`          | Server-rendered form, POST handled server-side; on success sign in (existing `ScaryCaveSignIn`) and redirect to the cave; on failure re-render with an error message (replaces `alert()`). Keeps the "accounts expire after one day / names letters only, ≥4 chars" notice and the link to register. |
| `/register`       | Same shape as login, using `AccountSession.Register`. Failure message: "Registration failed - try a different name." |
| `/scarycave`      | The cave page (below). Requires auth; unauthenticated → redirect to login.                  |
| `/logout`         | A POST (navbar item becomes a small form/button) that calls `IAccountActor.Logout`, signs out, redirects home. |
| `/startover`      | A POST that calls `IPlayerActor.StartOver` and redirects to the cave.                       |
| `/about`          | Static page; update the "Interesting Technology" list (drop next.js/React, add Razor Components + htmx). |
| `/health`         | Server-rendered: Orleans/Redis status via the existing `ITestGrain.Ping`, plus whether a reCAPTCHA site key is configured. Drop the API/Next base-URL lines, which no longer mean anything. |

Logout and Start Over are POSTs with antiforgery, not GETs.

### reCAPTCHA v3

Keep it on login and register. The server already verifies tokens (`IReCaptchaService`).
The page loads `https://www.google.com/recaptcha/api.js?render=<siteKey>` and, on submit, calls
`grecaptcha.execute(siteKey, {action: "login"|"register"})`, puts the token in a hidden field,
then submits the form normally. The site key is public and comes from new config
`ScaryCave:ReCaptchaSiteKey` (rendered into the page). Health reports whether it is set.

**Decided:** the site key is committed in `ScaryCavesWeb/appsettings.json` (it is public).
Copy the value of `NEXT_PUBLIC_RECAPTCHA_SITE_KEY` out of `scarycaves-next/.env` *before*
deleting that directory — `.env` is the only place it is recorded. Remove the misleading
`NEXT_PUBLIC_RECAPTCHA_SITE_KEY_FILE` wiring along with the `scary_next` service; nothing in
`stack.yml` needs to set the site key.

### The cave page

Content, same as today:
- `Room {id}: {name}` heading and description
- Mob descriptions in the room
- "Other Players:" list (excluding yourself), hidden when empty
- "Some things you might do:" and a `GO {direction}` button per exit
- "Your Name: …"
- A "Player Messages" column with the last 10 messages, newest first, each with a 24-hour
  `HH:mm:ss` timestamp. Moving to a new room clears the messages (today's `UpdatePlayerView`
  behaviour). Messages: "X has entered the room.", "X has left the room.", the same for mobs,
  "Welcome X to the Scary Cave!", "You can't go that way!", "Invalid direction",
  "Error moving player".

## Live updates: SSE + htmx replaces SignalR

`GameHub`, `AddSignalR`, and `IHubContext` usage are removed.

**Stream.** The cave page opens an authenticated SSE stream (e.g. `GET /cave/stream`) with
`hx-ext="sse"`. Each open stream gets a server-generated stream id that takes the place of
today's SignalR connection id:
- On open: `IPlayerActor.BeginSession(streamId)` (as `OnConnectedAsync` does today), then send
  the full room panel and the welcome message.
- On close (client gone / request aborted): `IPlayerActor.EndSession` (as `OnDisconnectedAsync`).
- htmx's SSE extension reconnects on its own; a reconnect is just a new stream and a new
  session, which re-sends the full room.

**Room broadcaster (replaces SignalR groups).** A singleton service holding
room location → set of stream ids → bounded per-stream channel (EliteEvents' `LiveEventHub`
`ClientSubscription` pattern is the model). `RoomActor` calls it instead of `IHubContext`:
add/remove a stream to a room on player enter/leave, and publish on player/mob enter/leave.

**What gets pushed.** HTML, not JSON. On any change to a room, the broadcaster sends every
stream in that room:
1. the re-rendered room panel (`sse-swap` replaces it wholesale), and
2. a message `<li>` (`hx-swap="afterbegin"` into the messages list).

Re-rendering the whole panel instead of patching lists keeps the client dumb; `RoomActor`
already has the complete `Room` at every publish point, so it can hand that over. Fragments
are rendered with Razor components (EliteEvents' `TickerFragmentRenderer` / `HtmlRenderer`
approach).

**Moving.** Exit buttons use `hx-post` (e.g. `/cave/move/{direction}`) with the antiforgery
token sent as a header. The response is the new room panel (and clears messages); a refusal
returns just the message. Hub logic from `GameHub.MoveTo` moves to that endpoint unchanged in
spirit.

**Auth expiry / errors.** If the stream fails or comes back 401, the page reloads, which lets
the normal auth redirect send the player to login (replaces `onclose → router.push('/login')`).
A small script keeps the message list at 10 items, like `ticker.js` trims the ticker.

**Two tabs, same player (builder's call, recommended).** Today a second connection overwrites
`Player.ConnectionId`, and closing either tab ends the session. Make `EndSession` a no-op
unless the closing stream id is still the player's current one, so the newest tab wins and
closing a stale tab doesn't pull the player out of the room. Older streams just stop getting
room events.

**Per-viewer rendering (my call).** The panel differs by viewer ("Other Players" leaves out
the viewer, "Your Name"), so the broadcaster renders it per stream, or renders the shared part
once and the viewer-specific bits per stream. A handful of players per room makes this cheap.

**Logout (my call).** `AccountActor.Logout` must end the player's session whatever stream id
is current, so logging out always takes the player out of the room.

## Server cleanup

- Delete `GameHub`, SignalR registration, the CORS policy, and `HomeController`'s JSON actions
  (`Login`, `Register`, `Logout`, `StartOver`, `Status`, `Health`, `AccessDenied`, `Error`)
  once their replacements exist. `LoginModel`/`RegisterModel` may be reused as form models,
  minus the JSON-only bits. `ErrorViewModel` and `AddControllersWithViews` go if nothing uses them.
- Cookie auth: `LoginPath`/`AccessDeniedPath` point at the new login page. Keep HttpOnly,
  `SameSite=Strict` (fine now that everything is same-origin), and the expiry.
- `Program.cs` gets `AddRazorComponents()` (no interactive services), `UseAntiforgery()`,
  `MapStaticAssets()`, `MapRazorComponents<App>()`.
- Production runs behind nginx doing TLS. Add forwarded-headers handling so the app knows the
  scheme is https, and make sure `UseHttpsRedirection` doesn't loop behind the proxy.

## Static assets (vendored, committed)

`wwwroot/lib/htmx/htmx.min.js`, `wwwroot/lib/htmx/sse.js` (htmx 2.x and its SSE extension;
copy the same versions EliteEvents uses unless a newer stable one is obviously better),
`wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` (Bootstrap 5.3; the CSS is enough, since the
navbar has no collapse toggle), `scary_cave_background.jpg`, a favicon if there is one. Port
`globals.css` to `wwwroot/app.css`, dropping anything Tailwind-only. The login/register notice
box currently uses Tailwind classes; restyle it as a Bootstrap alert.

## .NET 10

- `ScaryCavesWeb.csproj` and `ScaryCavesTest.csproj` → `net10.0`.
- Orleans packages → the current stable release that supports .NET 10. Redis-stored state is
  ephemeral (accounts expire in a day; world state is rebuilt from `scary-cave.json`), so a
  storage-format change across Orleans versions is acceptable: flushing Redis on deploy is fine.
- Test-project packages → current versions.
- `WarningsAsErrors` stays on.

## Deployment changes

- `ScaryCavesWeb/Dockerfile`: SDK 10 for build, ASP.NET 10 runtime image for the final stage
  (it currently runs on the full SDK image).
- `stack.yml`: remove `scary_next`.
- `build_images.sh`, `push_images.sh`, `deploy_images.sh`, `.github/workflows/build-push-images.yml`:
  remove every Next step.
- nginx config (`scarycaves` file in the repo): send all traffic to `127.0.0.1:8000`, drop the
  `/api` location and rewrite, and turn off proxy buffering for the SSE stream
  (`proxy_buffering off`, or set `X-Accel-Buffering: no` from the stream endpoint). The
  operator applies it on the server.
- Delete `scarycaves-next/` entirely.
- README: rewrite the front-end, local-settings, and Docker/nginx sections to match; one
  process on port 8000, no `.env`.

### One-time cutover (decided: the operator does this by hand)

The deploy scripts are **not** changed to handle the cutover (no `--prune`, no Redis flush in
`deploy_images.sh`). Instead, the builder writes an operator checklist as
`docs/cutover-checklist.md` (and links it from the README), covering at least:

1. Build and push the new `scary_aspnet` image (Next image no longer built).
2. On the server, remove the old Next service: `docker service rm scarycaves_scary_next`
   (or `docker stack deploy --prune …` once).
3. Flush the game's Redis data (accounts/players expire in a day anyway; world state is
   rebuilt from `scary-cave.json`), so the upgraded Orleans doesn't trip over old-format state.
   Give the exact command for the swarm's `redis` service.
4. Deploy the new stack (`deploy-only.yml` workflow or `deploy_images.sh`).
5. Install the updated nginx config from the repo's `scarycaves` file, `nginx -t`, reload.
6. Smoke test: `/health`, register, log in, move rooms, a second player sees enter/leave.
7. Optionally remove the `trasa/scary_next` images and the now-unused swarm secret wiring.

## Testing

`ScaryCavesTest` has only a placeholder today. At minimum, add unit tests for the room
broadcaster (subscribe/unsubscribe, publish reaches only that room's streams, a slow client
drops oldest instead of blocking) and for the `EndSession` stale-stream rule. Fragment
rendering tests are welcome. Then do a manual end-to-end check: register, log in, move between
rooms, open a second browser as another player and watch enter/leave messages and mobs
wandering, start over, log out, `/health`.

## Left to the builder

Exact routes and component breakdown; the SSE event names; whether login/register use
`[SupplyParameterFromForm]` in the component or a minimal-API POST; how the antiforgery token
reaches htmx; fragment-renderer details; file layout under `Components/`; how timestamps are
formatted (server time is fine).
