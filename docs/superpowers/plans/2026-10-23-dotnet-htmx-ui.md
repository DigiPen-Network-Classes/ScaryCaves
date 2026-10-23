# Scary Caves Razor Components + htmx UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Serve the whole Scary Caves UI from `ScaryCavesWeb` with static-SSR Razor Components, htmx, and an SSE stream, then delete `scarycaves-next` and every trace of npm.

**Architecture:** Static SSR pages (no interactive render mode) following `~/prj/EliteDangerous/EliteEvents/EliteEvents.Web`. An in-process `RoomBroadcaster` singleton replaces SignalR groups: `RoomActor` publishes `RoomEvent` data, and the per-player SSE endpoint renders it to HTML with Razor components *for that viewer* before writing it out. Exit buttons `hx-post` to a move endpoint that returns the new room panel.

**Tech Stack:** .NET 10, ASP.NET Core Razor Components (static SSR), Orleans 10.4.0 + Redis persistence, htmx 2.x + SSE extension (vendored), Bootstrap 5.3 CSS (vendored), reCAPTCHA v3, xunit.

**Spec:** `docs/superpowers/specs/2026-10-23-dotnet-htmx-ui-design.md`

## Global Constraints

- No npm, no `package.json`, no Node in any Dockerfile, no JS build step, no libman. Vendored files are copied and committed.
- No Blazor interactive render modes: no `AddInteractiveServerComponents`, no `@rendermode`, no `blazor.web.js`.
- No CDN for scripts/styles. The one exception is `https://www.google.com/recaptcha/api.js`.
- `net10.0` for `ScaryCavesWeb` and `ScaryCavesTest`; `WarningsAsErrors` stays `true`.
- Orleans packages `Microsoft.Orleans.Server` and `Microsoft.Orleans.Persistence.Redis` at `10.4.0`.
- reCAPTCHA site key lives in `ScaryCavesWeb/appsettings.json` as `ScaryCave:ReCaptchaSiteKey`, copied from `NEXT_PUBLIC_RECAPTCHA_SITE_KEY` in `scarycaves-next/.env` **before** that directory is deleted. Do not echo the value into chat or commit messages.
- Logout and Start Over are POSTs with antiforgery validation.
- Message text, exactly: `"{name} has entered the room."`, `"{name} has left the room."` (players and mobs alike), `"Welcome {name} to the Scary Cave!"`, `"You can't go that way!"`, `"Invalid direction"`, `"Error moving player"`, register failure `"Registration failed - try a different name."`, login failure `"Login failed"`.
- Message list: newest first, max 10, each suffixed ` (HH:mm:ss)` 24-hour, server time.
- Login/register notice copy (keep): "Since this is Just For Fun, all accounts expire after one day so you may need to re-register." / "Player Names are restricted to letters only and must be at least 4 characters long."
- Look matches today: Bootstrap, `scary_cave_background.jpg` wrapper, navbar (The Scary Cave · Start Over · Logout · About · Project → `https://github.com/DigiPen-Network-Classes/ScaryCaves`), footer `© 2024 - Tony Rasa`, Comic Sans body / Papyrus headings from `globals.css`.

## Review Focus

1. **Same player in two tabs** → the newest tab gets room events; the old tab's stream is closed with `superseded` and does *not* reconnect, its closing does not remove the player from the room, and other players see no spurious "entered"/"left" lines. Pinned in Task 3 (`Claim_SamePlayerTwice_SupersedesFirst`), Task 2 (`OwnsSession_*`), and the ordering rule in Task 7 Step 2.
2. **HTML containing newlines in an SSE frame** → every line is prefixed `data: `, so a multi-line room panel arrives whole. Pinned in Task 4 (`Format_MultilineHtml_PrefixesEveryLine`).
3. **Player names/descriptions with `<`/`&`** → rendered escaped, never as markup (Razor does this; don't build HTML by string concat). Pinned in Task 4 (`RoomPanel_EscapesMarkupInNames`).
4. **A slow or stuck browser** → its queue drops oldest events instead of blocking `RoomActor`. Pinned in Task 3 (`Publish_FullQueue_DropsOldestWithoutBlocking`).
5. **Cookie expired while on the cave page** → stream returns 401, page reloads, auth redirect lands on `/login` rather than a dead page. Covered by the manual check in Task 7 (behaviour lives in `cave.js` + `RequireAuthorization`).

---

## File Structure

```
ScaryCavesWeb/
  Program.cs                              (modify: Razor Components, antiforgery, forwarded headers, endpoints; drop SignalR/CORS/MVC)
  ScaryCavesWeb.csproj                    (modify: net10.0, Orleans 10.4.0)
  Dockerfile                              (modify: sdk:10.0 build, aspnet:10.0 final)
  appsettings.json                        (modify: ReCaptchaSiteKey)
  Services/ScaryCaveSettings.cs           (modify: ReCaptchaSiteKey)
  Services/ServiceDependency.cs           (modify: X509CertificateLoader, register broadcaster/renderer)
  Live/RoomEvent.cs                       (create)
  Live/RoomBroadcaster.cs                 (create: IRoomBroadcaster + impl + StreamSubscription)
  Live/SseFormatter.cs                    (create)
  Live/FragmentRenderer.cs                (create)
  Endpoints/CaveEndpoints.cs              (create: /cave/stream, /cave/move/{direction})
  Endpoints/AccountEndpoints.cs           (create: /logout, /startover)
  Actors/RoomActor.cs                     (modify: IHubContext → IRoomBroadcaster)
  Actors/PlayerActor.cs                   (modify: EndSession(string?))
  Actors/AccountActor.cs                  (modify: EndSession(null))
  Models/Player.cs                        (modify: OwnsSession)
  Components/App.razor, Routes.razor, _Imports.razor
  Components/Layout/MainLayout.razor, NavMenu.razor
  Components/Pages/Home.razor, About.razor, Health.razor, Login.razor, Register.razor, Cave.razor, NotFound.razor, Error.razor
  Components/Shared/RoomPanel.razor, MessageItem.razor, CaptchaForm.razor (script include)
  wwwroot/app.css, wwwroot/scary_cave_background.jpg, wwwroot/js/cave.js, wwwroot/js/captcha.js
  wwwroot/lib/htmx/htmx.min.js, wwwroot/lib/htmx/sse.js, wwwroot/lib/bootstrap/dist/css/bootstrap.min.css
  Hubs/GameHub.cs, Controllers/HomeController.cs, Models/ErrorViewModel.cs   (delete)
ScaryCavesTest/
  ScaryCavesTest.csproj                   (modify: net10.0, current packages)
  UnitTest1.cs                            (delete)
  PlayerSessionTests.cs, RoomBroadcasterTests.cs, SseFormatterTests.cs, FragmentRenderingTests.cs   (create)
docs/cutover-checklist.md                 (create)
scarycaves (nginx), stack.yml, build_images.sh, push_images.sh, deploy_images.sh,
.github/workflows/build-push-images.yml, README.md                           (modify)
scarycaves-next/                          (delete)
```

---

### Task 1: Upgrade to .NET 10 and Orleans 10.4.0

**Files:**
- Modify: `ScaryCavesWeb/ScaryCavesWeb.csproj`, `ScaryCavesTest/ScaryCavesTest.csproj`, `ScaryCavesWeb/Dockerfile`, `ScaryCavesWeb/Services/ServiceDependency.cs`

**Interfaces:** Produces a solution that builds and runs on `net10.0`. No API changes.

- [ ] **Step 1:** Set `<TargetFramework>net10.0</TargetFramework>` in both csproj files. Orleans packages → `10.4.0`. Test packages → `coverlet.collector 6.0.4`, `Microsoft.NET.Test.Sdk 17.14.1`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4` (the versions EliteEvents uses).
- [ ] **Step 2:** Run `dotnet build ScaryCaves.sln`. Expected: errors from `WarningsAsErrors` for obsolete APIs. Known one: `new X509Certificate2(path, password)` in `AddScaryDataProtection` → replace with `X509CertificateLoader.LoadPkcs12FromFile(path, password)`. Fix every error at its source; do not suppress warnings. If an Orleans 8→10 API break shows up (e.g. Redis storage option names), fix it to the 10.x API.
- [ ] **Step 3:** Run `dotnet build ScaryCaves.sln && dotnet test ScaryCaves.sln`. Expected: build succeeds with 0 warnings, placeholder test passes.
- [ ] **Step 4:** Dockerfile: build stage `mcr.microsoft.com/dotnet/sdk:10.0`, final stage `FROM mcr.microsoft.com/dotnet/aspnet:10.0` (not the SDK), keep `EXPOSE 8000`, same publish/entrypoint. Run `docker build -t scary_aspnet:test ./ScaryCavesWeb` if Docker is available; expected: succeeds. If Docker is not running, say so in the task report.
- [ ] **Step 5:** With local Redis running (`redis-cli ping` → `PONG`), run `dotnet run --project ScaryCavesWeb` and `curl -s localhost:8000/Home/Health`. Expected: `{"redis":true,"orleans":true}`. If Redis holds old Orleans 8 state that fails to load, `redis-cli FLUSHDB` on the dev DB is acceptable (spec: state is ephemeral).
- [ ] **Step 6: Commit** — `git commit -am "Upgrade to .NET 10 and Orleans 10.4.0"`

---

### Task 2: Session ownership rule

**Files:**
- Modify: `ScaryCavesWeb/Models/Player.cs`, `ScaryCavesWeb/Actors/PlayerActor.cs`, `ScaryCavesWeb/Actors/AccountActor.cs`
- Delete: `ScaryCavesTest/UnitTest1.cs`
- Test: `ScaryCavesTest/PlayerSessionTests.cs`

**Interfaces:**
- Produces: `bool Player.OwnsSession(string? connectionId)` — `true` when `connectionId` is `null` (unconditional end, used by logout) or equals a non-empty `ConnectionId`; otherwise `false`.
- Produces: `IPlayerActor.EndSession(string? connectionId)` (alias stays `"EndSession"`) — returns `null` and changes nothing when `!Player.OwnsSession(connectionId)`; otherwise behaves as today.
- Rename the `ConnectionId` doc comment from "SignalR Connection ID" to "Id of the player's current SSE stream". Keep the property name and `[Id(4)]`.

- [ ] **Step 1: Write the failing tests**

```csharp
public class PlayerSessionTests
{
    private static Player WithConnection(string? id) => new(Guid.NewGuid(), "Bob", 0, "scary-cave", id);

    [Fact] public void OwnsSession_SameId_True() => Assert.True(WithConnection("s1").OwnsSession("s1"));
    [Fact] public void OwnsSession_OtherId_False() => Assert.False(WithConnection("s2").OwnsSession("s1"));
    [Fact] public void OwnsSession_Null_AlwaysTrue() => Assert.True(WithConnection("s2").OwnsSession(null));
    [Fact] public void OwnsSession_NoCurrentSession_False() => Assert.False(WithConnection(null).OwnsSession("s1"));
}
```

- [ ] **Step 2:** `dotnet test --filter PlayerSessionTests` → FAIL (`OwnsSession` not defined).
- [ ] **Step 3:** Implement `OwnsSession`; change `EndSession` signature and guard; `AccountActor.Logout` calls `EndSession(null)`; `GameHub.OnDisconnectedAsync` (deleted in Task 5) calls `EndSession(Context.ConnectionId)` so the build stays green.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5: Commit** — `"Only the current stream may end a player's session"`

---

### Task 3: RoomBroadcaster

**Files:**
- Create: `ScaryCavesWeb/Live/RoomEvent.cs`, `ScaryCavesWeb/Live/RoomBroadcaster.cs`
- Modify: `ScaryCavesWeb/Services/ServiceDependency.cs` (register `IRoomBroadcaster` → `RoomBroadcaster` singleton in `AddScaryCaveWeb`)
- Test: `ScaryCavesTest/RoomBroadcasterTests.cs`

**Interfaces (Produces):**

```csharp
namespace ScaryCavesWeb.Live;

public enum RoomEventKind { RoomChanged, Superseded }

/// Room is the room *after* the change; Message is the line for the message list (null = none).
public sealed record RoomEvent(RoomEventKind Kind, RoomState? Room, string? Message);

public interface IRoomBroadcaster
{
    /// New stream for this player. Does not touch other streams yet.
    StreamSubscription Subscribe(string playerName);
    /// Make `subscription` the player's current stream. Any earlier subscription for the same
    /// player gets a Superseded event and its channel is completed. The stream endpoint calls
    /// this only *after* BeginSession(subscription.StreamId) returns, so by the time the old
    /// stream's EndSession(oldId) reaches the grain, the player's ConnectionId is already the
    /// new id and the call is a no-op (Task 2's rule). Calling it the other way round would race.
    void Claim(StreamSubscription subscription);
    void Join(string streamId, Location room);
    void Leave(string streamId, Location room);
    /// Writes to every stream joined to `room`. Never blocks.
    void Publish(Location room, RoomEvent roomEvent);
}

public sealed class StreamSubscription : IDisposable
{
    public string StreamId { get; }            // Guid.NewGuid().ToString("N")
    public string PlayerName { get; }
    public ChannelReader<RoomEvent> Reader { get; }
    public void Dispose();                     // removes from every room and from the player map
}
```

Per-stream channel: `Channel.CreateBounded<RoomEvent>(32)` with `FullMode = DropOldest`, `SingleReader = true` (mirror EliteEvents' `LiveEventHub`). Use `ConcurrentDictionary` for stream id → channel, location → set of stream ids, player name → current stream id. Unknown stream ids in `Join`/`Leave`/`Publish` are ignored.

- [ ] **Step 1: Write the failing tests** (helper `Location A = new(1, "z"), B = new(2, "z")`, `RoomEvent Ev(string m) => new(RoomEventKind.RoomChanged, null, m)`):
  - `Publish_ReachesOnlyStreamsInThatRoom` — s1 joins A, s2 joins B; publish to A; `s1.Reader.TryRead` yields `Message == "x"`, `s2.Reader.TryRead` is false.
  - `Leave_StopsDelivery` — join A, leave A, publish A → nothing read.
  - `Dispose_RemovesFromRooms` — join A, dispose, publish A → no exception, and `s1.Reader.Completion.IsCompleted` is true.
  - `Claim_SamePlayerTwice_SupersedesFirst` — first = Subscribe("Bob"), Claim(first), second = Subscribe("Bob"); first has no event yet; Claim(second) → first reads one event with `Kind == Superseded`, then `first.Reader.Completion.IsCompleted`; second is open; `first.StreamId != second.StreamId`.
  - `Claim_OtherPlayer_DoesNotSupersede` — Bob and Alice each Subscribe + Claim → neither receives Superseded.
  - `Dispose_OfSupersededStream_DoesNotUnregisterNewer` — supersede as above, dispose first, second joins A, publish A → second receives it.
  - `Publish_FullQueue_DropsOldestWithoutBlocking` — join A, publish 40 events `"0".."39"` synchronously; drain; count == 32, first message `"8"`, last `"39"`.
- [ ] **Step 2:** `dotnet test --filter RoomBroadcasterTests` → FAIL (types not defined).
- [ ] **Step 3:** Implement `RoomEvent`, `IRoomBroadcaster`, `RoomBroadcaster`, `StreamSubscription`; register the singleton.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5: Commit** — `"Add in-process RoomBroadcaster for live room events"`

---

### Task 4: Fragments: SSE formatting and Razor rendering

**Files:**
- Create: `ScaryCavesWeb/Live/SseFormatter.cs`, `ScaryCavesWeb/Live/FragmentRenderer.cs`, `ScaryCavesWeb/Components/_Imports.razor`, `ScaryCavesWeb/Components/Shared/RoomPanel.razor`, `ScaryCavesWeb/Components/Shared/MessageItem.razor`
- Modify: `ServiceDependency.cs` (register `FragmentRenderer` singleton)
- Test: `ScaryCavesTest/SseFormatterTests.cs`, `ScaryCavesTest/FragmentRenderingTests.cs`

**Interfaces (Produces):**
- `static string SseFormatter.Format(string eventName, string data)` → `"event: {eventName}\n"` + one `"data: {line}\n"` per line of `data` (split on `\r\n`/`\n`) + `"\n"`.
- `sealed class FragmentRenderer : IAsyncDisposable` with `Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters) where TComponent : IComponent`. Same scope/disposal pattern as EliteEvents' `TickerFragmentRenderer` (one long-lived scope, a fresh `HtmlRenderer` per render).
- `RoomPanel.razor` — `[Parameter, EditorRequired] RoomState Room`, `[Parameter, EditorRequired] string ViewerName`. Root element `<div id="room-panel">`. Content and classes from `scarycaves-next/src/app/scarycave/page.tsx` and its components: `h1.room-name.display-4` "Room {Id}: {Name}", `p.room-description`, one `p.mob-description` per mob, `div.other-players` with "Other Players:" list excluding `ViewerName` (omitted when empty), `p.player-action` "Some things you might do:", `ul.player-action` with per-exit `<button type="button" class="btn btn-outline-primary player-action" hx-post="/cave/move/{Direction}" hx-target="#room-panel" hx-swap="outerHTML">GO {Direction}</button>`, `div.player-stats` "Your Name: {ViewerName}".
- `MessageItem.razor` — `[Parameter, EditorRequired] string Text`, `[Parameter, EditorRequired] DateTimeOffset At` → `<li>{Text} ({At:HH:mm:ss})</li>`.

- [ ] **Step 1: Write the failing tests**
  - `Format_SingleLine` — `Format("message", "<li>x</li>")` == `"event: message\ndata: <li>x</li>\n\n"`.
  - `Format_MultilineHtml_PrefixesEveryLine` — `Format("room", "<div>\n<p>a</p>\r\n</div>")` == `"event: room\ndata: <div>\ndata: <p>a</p>\ndata: </div>\n\n"`.
  - Fragment tests build a `ServiceCollection().AddLogging().BuildServiceProvider()` and a `FragmentRenderer` from it. Sample room: id 3, name "Hall", exits North and Up, players {"Bob","Alice"}, one mob with description "A bat hangs here."; render with viewer "Bob".
    - `RoomPanel_ShowsRoomAndExits` — html contains `Room 3: Hall`, `A bat hangs here.`, `hx-post="/cave/move/North"`, `GO Up`, `Your Name: Bob`.
    - `RoomPanel_OtherPlayersExcludesViewer` — contains `Alice`; the "Other Players" `<ul>` does not contain `<li>Bob</li>`.
    - `RoomPanel_AloneHidesOtherPlayers` — players {"Bob"} → html does not contain `Other Players:`.
    - `RoomPanel_EscapesMarkupInNames` — player `"<b>x</b>"` → html contains `&lt;b&gt;x&lt;/b&gt;` and not `<b>x</b>`.
    - `MessageItem_FormatsTimestamp` — `At = 2026-10-23T14:05:09Z` → `<li>Bob has entered the room. (14:05:09)</li>`.
- [ ] **Step 2:** `dotnet test --filter "SseFormatterTests|FragmentRenderingTests"` → FAIL.
- [ ] **Step 3:** Implement the formatter, renderer and the two components. Construct `RoomState` in tests via `new RoomState(new Room(...))` using the `Room` JSON constructor.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5: Commit** — `"Add server-rendered room fragments and SSE formatting"`

---

### Task 5: Actors publish through the broadcaster; remove SignalR

**Files:**
- Modify: `ScaryCavesWeb/Actors/RoomActor.cs`, `ScaryCavesWeb/Program.cs`
- Delete: `ScaryCavesWeb/Hubs/GameHub.cs`

**Interfaces:**
- Consumes: `IRoomBroadcaster` (Task 3), `RoomEvent`/`RoomEventKind` (Task 3), `EndSession(string?)` (Task 2).
- Produces: room events with these semantics (later tasks rely on them):
  - `EnterPlayer(p)`: add to room, `Publish(loc, RoomChanged, new RoomState(room), "{p.Name} has entered the room.")` to streams *already* there, **then** `Join(p.ConnectionId, loc)` — the mover does not get their own entered line. If `p.Name` was already in `PlayersInRoom` (second tab), skip the publish but still `Join`.
  - `Leave(Player p)`: remove from room, `Leave(p.ConnectionId, loc)` **then** publish `"{p.Name} has left the room."`.
  - `EnterMob`/`Leave(Mob)`: publish `"{mob.Name} has entered the room."` / `"... has left the room."` with the updated `RoomState`.

- [ ] **Step 1:** Replace `IHubContext<GameHub>` with `IRoomBroadcaster` in `RoomActor` per the semantics above. Delete `GameHub.cs`; remove `AddSignalR`, `MapHub`, and `using ScaryCavesWeb.Hubs` from `Program.cs`.
- [ ] **Step 2:** `dotnet build ScaryCaves.sln && dotnet test ScaryCaves.sln` → 0 warnings, all tests pass. `grep -rn "SignalR\|HubContext\|GameHub" ScaryCavesWeb --include='*.cs'` → no matches.
- [ ] **Step 3: Commit** — `"Route room events through RoomBroadcaster instead of SignalR"`

---

### Task 6: Site shell, static pages, auth pages

**Files:**
- Create: `Components/App.razor`, `Components/Routes.razor`, `Components/Layout/MainLayout.razor`, `Components/Layout/NavMenu.razor`, `Components/Pages/{Home,About,Health,Login,Register,NotFound,Error}.razor`, `Endpoints/AccountEndpoints.cs`, `wwwroot/app.css`, `wwwroot/js/captcha.js`, vendored `wwwroot/lib/...`, `wwwroot/scary_cave_background.jpg`
- Modify: `Program.cs`, `appsettings.json`, `Services/ScaryCaveSettings.cs`, `Models/LoginModel.cs`, `Models/RegisterModel.cs`
- Delete: `Controllers/HomeController.cs`, `Models/ErrorViewModel.cs`

**Interfaces:**
- Produces routes: `/`, `/about`, `/health`, `/login`, `/register`, `POST /logout`, `POST /startover`.
- Produces `ScaryCaveSettings.ReCaptchaSiteKey : string` (default `""`).
- Produces `App.razor` loading `lib/bootstrap/dist/css/bootstrap.min.css`, `app.css`, `lib/htmx/htmx.min.js`, `lib/htmx/sse.js` via `@Assets[...]`, and a `<HeadOutlet/>`; Task 7 relies on htmx + sse being on every page.

- [ ] **Step 1: Assets.** Copy `htmx.min.js` and `sse.js` from `~/prj/EliteDangerous/EliteEvents/EliteEvents.Web/wwwroot/lib/htmx/`, `bootstrap.min.css` (+ `.map`) from its `lib/bootstrap/dist/css/`, and `scarycaves-next/public/scary_cave_background.jpg`. Port `scarycaves-next/src/app/globals.css` to `wwwroot/app.css` minus the three `@tailwind` lines.
- [ ] **Step 2: Shell.** `App.razor`, `Routes.razor` (with `NotFoundPage`), `MainLayout.razor` reproducing `layout.tsx` (navbar, `div.background-wrapper`, `main`, footer), `NavMenu.razor` reproducing `Navbar.tsx` with Start Over and Logout as `<form method="post" action="/startover|/logout" class="d-inline"><AntiforgeryToken/><button class="nav-link btn btn-link">…</button></form>`. `Home.razor` and `About.razor` from `page.tsx` / `about/page.tsx`; in About replace the next.js line with "ASP.NET Core Razor Components and htmx - server-rendered UI".
- [ ] **Step 3: Program.cs.** Add `AddRazorComponents()` (no interactive), `AddAntiforgery()`; `Configure<ForwardedHeadersOptions>` for `XForwardedFor | XForwardedProto` with `KnownNetworks`/`KnownProxies` cleared (nginx is on the same host) and `app.UseForwardedHeaders()` first in the pipeline; remove `UseHttpsRedirection` (nginx terminates TLS and already redirects 80→443). Remove CORS, `AddControllersWithViews`, `MapControllerRoute`. Add `UseAntiforgery()`, `MapStaticAssets()`, `MapRazorComponents<App>()`, `MapAccountEndpoints()`. `UseExceptionHandler("/Error")`. Cookie options: `LoginPath = "/login"`, `AccessDeniedPath = "/login"`; keep HttpOnly/Strict/expiry. Change `SecurePolicy` to `CookieSecurePolicy.SameAsRequest` so local http dev works; behind nginx the forwarded proto makes it Secure.
- [ ] **Step 4: Settings.** Add `ReCaptchaSiteKey` to `ScaryCaveSettings` and `appsettings.json`, value copied from `scarycaves-next/.env` (`NEXT_PUBLIC_RECAPTCHA_SITE_KEY`).
- [ ] **Step 5: Login/Register.** Static-SSR forms: `<EditForm Model="Input" FormName="login" OnValidSubmit="…" method="post" id="captcha-form" data-action="login" data-site-key="@Settings.ReCaptchaSiteKey">` with `DataAnnotationsValidator`, `InputText` for name/password, hidden `InputText` for `Token` with `id="captcha-token"`. On valid submit: `CaptchaService.IsTokenValid` → `AccountSession.Login`/`Register` → `HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.AccountExpires))` → `Navigation.NavigateTo("/scarycave")`; otherwise set the page's error message (copy from Global Constraints) and re-render with a Bootstrap `alert-danger`. Notice box becomes a Bootstrap `alert alert-info` with the copy from Global Constraints and a link to the other form. Pages include `https://www.google.com/recaptcha/api.js?render={siteKey}` and `js/captcha.js`. `captcha.js`: on `#captcha-form` submit, `preventDefault`, `grecaptcha.ready → execute(siteKey, {action})`, set `#captcha-token`, then `form.submit()`. Keep `LoginModel`/`RegisterModel` validation attributes; they now bind from the form.
- [ ] **Step 6: AccountEndpoints.** `MapAccountEndpoints(this IEndpointRouteBuilder)`: `POST /logout` (validate antiforgery via `IAntiforgery.ValidateRequestAsync`; if an account id claim exists call `IAccountActor.Logout`; `SignOutAsync`; redirect `/`), `POST /startover` (`RequireAuthorization`; validate antiforgery; `IPlayerActor.StartOver`; redirect `/scarycave`). Logic is the old `HomeController` actions.
- [ ] **Step 7: Health.** `Health.razor`: Orleans/Redis via `ITestGrain.Ping` (catch → Bad), "Recaptcha Site Key: Good/Bad" by `ReCaptchaSiteKey` non-empty. Delete `HomeController.cs` and `ErrorViewModel.cs`; `Error.razor` shows the request id.
- [ ] **Step 8: Verify.** `dotnet build` (0 warnings) and `dotnet test`. Run the app; with `curl -s -o /dev/null -w "%{http_code}"`: `/`, `/about`, `/health`, `/login`, `/register` → 200; `/nope` → 404. (`/scarycave` arrives in Task 7.) Then in the browser: register a new player (reCAPTCHA runs on localhost if the key allows it; if Google rejects localhost, note it and verify with a key that does), log out, log back in.
- [ ] **Step 9: Commit** — `"Serve the site shell, static pages and login/register from Razor Components"`

---

### Task 7: The cave page, stream and move endpoints

**Files:**
- Create: `Components/Pages/Cave.razor`, `Endpoints/CaveEndpoints.cs`, `wwwroot/js/cave.js`
- Modify: `Program.cs` (`MapCaveEndpoints()`)

**Interfaces:**
- Consumes: `IRoomBroadcaster`, `StreamSubscription`, `RoomEvent` (Task 3); `SseFormatter.Format`, `FragmentRenderer`, `RoomPanel`, `MessageItem` (Task 4); `IPlayerActor.BeginSession(string)`, `EndSession(string?)`, `MoveTo(Direction)`.
- Produces: `GET /scarycave` (page), `GET /cave/stream` (SSE), `POST /cave/move/{direction}`. SSE event names: `room` (RoomPanel HTML), `message` (MessageItem HTML), `superseded` (empty data).

- [ ] **Step 1: Cave.razor** (`@page "/scarycave"`, `[Authorize]`). Layout from `scarycave/page.tsx`: `div.row` → `div.col-md-8.text-center` containing `<div id="room-panel" sse-swap="room" hx-swap="outerHTML">Entering the cave…</div>`; `div.col-md-4` with `div.player-messages` → "Player Messages:" and `<ul id="messages" sse-swap="message" hx-swap="afterbegin"></ul>`. Wrapping element: `hx-ext="sse" sse-connect="/cave/stream" sse-close="superseded"`. Put the antiforgery request token on `<body>`-level wrapper as `hx-headers='{"RequestVerificationToken":"…"}'` (get it from `IAntiforgery.GetAndStoreTokens(HttpContext)`). Include `js/cave.js`.
- [ ] **Step 2: Stream endpoint** — `GET /cave/stream`, `RequireAuthorization()`. Headers and keep-alive exactly as EliteEvents `StreamEndpoint` (`text/event-stream`, `no-cache, no-transform`, `X-Accel-Buffering: no`, `DisableBuffering`, `: ping` every 25 s). Flow, in this order: `using var sub = broadcaster.Subscribe(playerName)`; `var view = await player.BeginSession(sub.StreamId)`; `broadcaster.Claim(sub)`; write `room` (RoomPanel for `view.Room`, viewer = player name) and `message` ("Welcome {name} to the Scary Cave!"); then loop reading `sub.Reader`: `RoomChanged` → write `room` (if `Room` not null) and `message` (if `Message` not null); `Superseded` → write `superseded` and return. `finally` → `await player.EndSession(sub.StreamId)` (catch and log, like the old hub).
- [ ] **Step 3: Move endpoint** — `POST /cave/move/{direction}`, `RequireAuthorization()`, validate antiforgery. Invalid enum → 200 with `MessageItem("Invalid direction")` swapped into `#messages` via `HX-Retarget: #messages` and `HX-Reswap: afterbegin`. `MoveTo` returns null → same with "You can't go that way!". Exception → log, same with "Error moving player". Success → RoomPanel for the new room plus `<ul id="messages" hx-swap-oob="true"></ul>` (clears messages, matching today's `UpdatePlayerView`).
- [ ] **Step 4: cave.js** — like EliteEvents' `ticker.js`: on `htmx:sseMessage` trim `#messages` to 10 children. On `htmx:sseError`, `location.reload()` after a 2 s delay (an expired cookie makes the reload redirect to `/login`), unless a `superseded` event was seen. In that case, replace `#room-panel`'s content with "The Scary Cave is open in another window." and do nothing else.
- [ ] **Step 5: Verify.** `dotnet build` (0 warnings), `dotnet test`. Manual, two browsers (one private window), local Redis:
  1. Register A → lands on cave, room panel + welcome message.
  2. Register B → A sees "B has entered the room." and B under Other Players.
  3. B moves → B's panel changes and messages clear; A sees "B has left the room."
  4. Wait ≥ `MobActivityTimerSeconds` (15 s) → mobs wander in/out with messages.
  5. Open A in a second tab → first tab shows the "another window" note and stops; A still in the room for B; closing the old tab doesn't make B see "A has left".
  6. Start Over → back to room 0. Logout → B sees "A has left the room."
  7. Delete the auth cookie in A's devtools → within a few seconds the page reloads to `/login`.
  8. More than 10 events → list stays at 10.
- [ ] **Step 6: Commit** — `"Add the cave page with live SSE updates and htmx movement"`

---

### Task 8: Remove Next.js and update deployment

**Files:**
- Delete: `scarycaves-next/`
- Modify: `stack.yml`, `build_images.sh`, `push_images.sh`, `deploy_images.sh`, `.github/workflows/build-push-images.yml`, `scarycaves` (nginx), `README.md`
- Create: `docs/cutover-checklist.md`

- [ ] **Step 1:** Confirm `grep -c ReCaptchaSiteKey ScaryCavesWeb/appsettings.json` is 1 with a non-empty value, then `git rm -r scarycaves-next`.
- [ ] **Step 2:** Remove every `scary_next` / Next.js step from `stack.yml`, the three scripts, and `build-push-images.yml`. `deploy_images.sh` keeps no `--prune` (operator does the cutover by hand).
- [ ] **Step 3:** nginx `scarycaves`: single `location /` → `http://127.0.0.1:8000`, add `proxy_set_header X-Forwarded-Proto $scheme;`, `proxy_buffering off;`, `proxy_read_timeout 1h;`; drop `/api` and the Upgrade/Connection upgrade headers (no WebSockets now). Keep the redirect server blocks and certbot lines as they are.
- [ ] **Step 4:** `docs/cutover-checklist.md`, the seven steps from the spec's "One-time cutover" section, with concrete commands: `docker service rm scarycaves_scary_next`; Redis flush `docker exec $(docker ps -q -f name=scarycaves_redis) redis-cli FLUSHALL`; `nginx -t && systemctl reload nginx`; the smoke-test list from Task 7 Step 5 (short form).
- [ ] **Step 5:** README: rewrite "Front-End Client", "Local Client Settings", "Docker Swarm", "Nginx Configuration" and the technology list for one process on :8000 with Razor Components + htmx; reCAPTCHA section mentions `ScaryCave:ReCaptchaSiteKey` in `appsettings.json`; link the cutover checklist.
- [ ] **Step 6: Verify.** `git ls-files | grep -Ei 'package(-lock)?\.json|node_modules|\.tsx?$'` → no output. `grep -rni "next\|3000\|npm" stack.yml *.sh .github scarycaves ScaryCavesWeb/Dockerfile` → no matches. `dotnet build && dotnet test` pass. `docker build ./ScaryCavesWeb` succeeds if Docker is available.
- [ ] **Step 7: Commit** — `"Remove the Next.js client; single ASP.NET deployable"`
