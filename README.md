# The Scary Cave -- an HTTP based Text Adventure

HTTP is stateless. Let's build a (very!) simple text game with multiple users and state,
using HTTP to make all the pieces go, with server-rendered pages and a live event stream.

## About the Scary Cave

This is a simple text-based adventure game. It is a work in progress!
It was created to demonstrate some networking basics and to be some example code 
for the Computer Networks I & II classes (CS-260 and CS-261).
As such, it is not a complete game and may have some bugs or other issues; and is 
not intended to be a "complete and polished product."

The initial design used some rudimentary Razor Views and AspNet MVC to glue things 
together - which became insufficient once things like Mobiles and other players 
were added. That was replaced with SignalR for messaging and a next.js/TypeScript
Single Page Application - which worked, but needed its npm dependencies fixed
constantly. Now it's back to all-.NET: Razor Components render the pages on the
server, htmx handles the clicks, and room events arrive over Server-Sent Events.
There's no npm and no JavaScript build step at all.

## Interesting Technology

* [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) - the web server
* [Microsoft Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/overview) - for managing game state
* [Razor Components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/) (static server-side rendering) - the pages
* [htmx](https://htmx.org) and its SSE extension - clicks and live room updates, vendored under `ScaryCavesWeb/wwwroot/lib`
* [Redis](https://redis.io) - Game State Database

# Local Development / Getting Started

There's a few components here that are needed to get this running.

## Database: Redis

I just used a local redis install for development. You can install it 
via `brew install redis` on a mac.

The demands on the redis database are quite small. You could run it in a docker 
image if you wanted, or really ... basically anything.

The redis connection string is specified in the 'appsettings.json' file and can
be overridden by the environment variable `ScaryCave__RedisConnectionString`.

## The Server

Everything is one ASP.NET Core app, written in C# (see the `ScaryCavesWeb` project).
It serves the pages, runs the Orleans Silo which manages game state through Actors,
and streams room events to each player's browser.

The server runs on port 8000 by default: open http://localhost:8000.

### Running the Server

You can run the server from the command line with `dotnet run --project ScaryCavesWeb`.
It should also run just fine in the IDE of your choice using  the `ScaryCavesWeb` profile:

```json
{  
  "profiles": {
    "ScaryCavesWeb": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:8000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Data Protection Keys and other Secrets

For local development, this project uses the 'UserSecrets' feature of dotNet.
You will need to set the two secrets that are used in the web project:

```bash
dotnet user-secrets set "ScaryCave:ReCaptchaSecretKey" "abcdefg..."
dotnet user-secrets set "ScaryCave:DataProtectionCertPassword" "hijklmnop..."
```

The server uses the Data Protection API and needs a certificate.  

Generating a self-signed certificate key-pair:

```bash
openssl req -x509 -nodes -newkey rsa:4096 \
  -keyout dp-key.pem \
  -out dp-cert.pem \
  -days 365 \
  -subj "/C=US/ST=State/L=City/O=Organization/OU=Unit/CN=example.com"
```

Convert that pem to pfx, because that seemed to work better:
```bash
openssl pkcs12 -export -out dp-cert.pfx -inkey dp-key.pem -in dp-cert.pem -passout pass:the_files_secret_goes_here
```

If you change certificates, you should delete the old .xml files in the /certs
directory (not sure if they'll still work).

#### Docker Secrets

For production, we use Docker Secrets for secret things (vs. paying money for 
Azure KeyVault or something sensible). The secrets have to be defined from the 
command line using Docker Swarm and show up on the container as files in the 
`/run/secrets` directory.

```bash
# NOTE! Don't use 'echo' for secrets, it adds a newline.
# only use 'echo -n' or better yet, printf (see man echo)
printf "myrecaptchasecret" | docker secret create recaptcha_secret_key -
docker secret create dp_cert.pfx dp_cert.pfx
printf "mypfxsecretpassword" | docker secret create dp_cert_password -
```

### Orleans and Actors

The actors store their state in Redis. If there's no activity for 15 minutes, the
actors will deactivate themselves. If there's no player activity, then the Mobs
will sleep (and stop moving about and consuming CPU). When someone logs in 
or activates their session, this automatically wakes up the Mobs and Rooms.

## The Web UI

The pages are Razor Components in `ScaryCavesWeb/Components`, rendered on the server
(static SSR - no Blazor circuit, no `blazor.web.js`).

* The cave page (`/scarycave`) opens one Server-Sent Events stream, `/cave/stream`.
  Room actors publish to an in-process `RoomBroadcaster` (`ScaryCavesWeb/Live`), and
  each player's stream renders the room panel and messages as HTML for htmx to swap in.
* The exit buttons are htmx POSTs to `/cave/move/{direction}`.
* htmx, its SSE extension and Bootstrap's CSS are plain files checked in under
  `ScaryCavesWeb/wwwroot/lib`. To upgrade one, download the new file and replace it.

### ReCaptcha v3

The login and register pages use Google's ReCaptcha v3 to help prevent bots from spamming the server
and being a nuisance. The secret key is stored ... secretly. ReCaptcha v3 doesn't
require any user interaction, and returns a score of humanness. (0.0 you are a robot,
1.0 you are a human.) 50% seemed like a good-enough value.

The site key is public and lives in `appsettings.json` as `ScaryCave:ReCaptchaSiteKey`.
The secret key is not, and comes from user secrets or a Docker secret (see above).

## Various Settings

See [ScaryCavesWeb/appsettings.json](ScaryCavesWeb/appsettings.json) for the 
settings that you might want to override. 

* `RedisConnectionString` - where to find the Redis database 
* `AccountTimeToLiveSeconds` - Since this is just for fun, accounts and players expire after this amount of time.
* `MobActivityTimer` - Controls how often a Mob wakes up and decides what to do. Lower value makes a more interesting demo, but requires more cpu.
* `DefaultZoneId` and `DefaultRoomId` - Where player start (or reset) to.
* `ReCaptchaSiteKey` - the public reCAPTCHA v3 site key, rendered into the login and register pages.
* `ReCaptcha` - the secret key should be loaded from an environment variable for production or from DotNet User Secrets for development. The threshold is how human one must be to pass. 
* `DataProtectionKeyPath` - where should the AspNet app write its Data Protection XML files? This should be a secure location.
* `DataProtectionCert` - See the section on Data Protection Keys and other Secrets above.

# Docker Swarm

See [build_images.sh](build_images.sh) for how to build the "production" image.
There is one Dockerfile, for `scary_aspnet`.

After building, [push_images.sh](push_images.sh) will push the latest image to
Docker Hub (public repository).

See [stack.yml](stack.yml) for the Swarm Production configuration. That will also
show how to configure the secrets (and other settings) via environment variables.

To deploy the stack, use `docker stack deploy -c stack.yml scarycaves`. 
See [deploy_local_stack.sh](deploy_local_stack.sh) for an example.

Moving off the old two-container (Next.js + ASP.NET) setup? See
[docs/cutover-checklist.md](docs/cutover-checklist.md) for the one-time steps.

# Nginx Configuration

The Nginx configuration is in [scarycaves](scarycaves). It's a pretty basic
https termination and proxy setup. 

Since I can't make up my mind if this is 'scary cave' or 'scary caves', we
forward both to the same place, then redirect the singular over to 'scarycaves'.
The TLS certificate works for both.

All traffic goes to the ASP.NET app on port 8000. Proxy buffering is off so the
Server-Sent Events stream (`/cave/stream`) reaches the browser as it happens, and
`X-Forwarded-Proto` tells the app the original request was https.

Using 'localhost' instead of 127.0.0.1 caused all sorts of IPv6 issues.
Every unique request was timing out for a full 60 seconds before falling back
to IPv4. So, don't use that; use 127.0.0.1 instead.

# Production Notes

Viewing the logs of the docker containers:
```bash
docker service logs scarycaves_scary_aspnet -f 
```

Docker commands requiring sudo? Make sure you're in the 'docker' group:
```bash
getent group docker || sudo groupadd docker
```

## Production Docker

See what's happening in a container:
```bash
docker ps
docker exec -it <containerId> sh  # and other commands ...
```

Temporariliy install net tools for debugging a container's issues:
```bash
docker exec -it <containerid> apt update
docker exec -it <containerId> apt install iproute2 -y
# or, for alpine:
docker exec -it <containerId> apk add iproute2
```

## Production nginx

After changing the config, make sure you didn't do anything stupid:
```bash
sudo nginx -t
```

Changed the config? Want to restart nginx?
```bash
sudo systemctl reload nginx
sudo systemctl restart nginx
```

Nginx logs:
```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```
