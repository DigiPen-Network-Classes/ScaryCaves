# Cutover: Next.js + ASP.NET → ASP.NET only

One-time steps for the first production deploy after the Next.js client was removed.
The deploy scripts don't do these for you (no `--prune`, no Redis flush), so run them
by hand, in order, on `scarycaves.meancat.com` unless noted.

1. **Build and push the new image** (from GitHub: run the *Build and Push Images* workflow
   with the new version; or locally `./build_images.sh && ./push_images.sh`). Only
   `scary_aspnet` is built now.

2. **Remove the old Next.js service.** Taking it out of `stack.yml` doesn't stop it, because
   `docker stack deploy` runs without `--prune`:

   ```bash
   docker service rm scarycaves_scary_next
   ```

3. **Stop the old ASP.NET app, then flush the game's Redis data.** Orleans went from 8.x to
   10.x and the new version won't read state the old one stored. Stop the old app *first*:
   while it runs, its mobs and players keep writing old-format state every few seconds, which
   would undo the flush. Nothing here is precious: accounts and players expire after a day, and
   the world is rebuilt from `scary-cave.json` on startup. Everyone will need to re-register
   (anyone still holding a login cookie is signed out the next time their cave page connects).

   ```bash
   docker service scale scarycaves_scary_aspnet=0
   docker exec $(docker ps -q -f name=scarycaves_redis) redis-cli FLUSHALL
   ```

4. **Deploy the new stack:** run the *Deploy to Production* workflow with the same version
   (or `VERSION=<version> DOCKER_USERNAME=trasa ./deploy_images.sh` on the server). The
   stack's `replicas: 1` brings `scary_aspnet` back up on the new image; check with
   `docker service ls`.

5. **Install the new nginx config.** Copy the repo's [`scarycaves`](../scarycaves) file over
   the site config on the server (all traffic now goes to port 8000; the `/api` location is
   gone; SSE needs `proxy_buffering off`), then:

   ```bash
   sudo nginx -t && sudo systemctl reload nginx
   ```

6. **Smoke test** at https://scarycaves.meancat.com:
   - `/health` shows Redis, Orleans and Recaptcha Site Key all **Good**
   - register a new player → lands in Room 0 with a welcome message
   - GO North → the room changes
   - in a second browser (or private window), register another player and walk into the
     same room → the first player sees "… has entered the room." and the name under Other Players
   - wait ~15 s in rooms 2/3 → the Giant Rat's comings and goings show up
   - Start Over → back to Room 0; Logout → home page

7. **Optional cleanup:** delete the old `trasa/scary_next` images from Docker Hub and any
   `scary_next` images left on the server (`docker image prune`).
