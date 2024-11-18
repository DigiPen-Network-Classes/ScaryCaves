# The Scary Cave -- an HTTP based Text Adventure

HTTP is stateless. Let's build a (very!) simple text game with multiple users and state,
using HTTP to make all the pieces go and using a Single Page Application as a client.

## About the Scary Cave

This is a simple text-based adventure game. It is a work in progress!
It was created to demonstrate some networking basics and to be some example code 
for the Computer Networks I & II classes (CS-260 and CS-261).
As such, it is not a complete game and may have some bugs or other issues; and is 
not intended to be a "complete and polished product."

The initial design used some rudimentary Razor Views and AspNet MVC to glue things 
together - which became insufficient once things like Mobiles and other players 
were added. Fortunately, replacing that with SignalR for messaging and 
basic next.js/TypeScript Single Page Application was (mostly!) straightforward.

## Interesting Technology

* [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) - for the "Server API"
* [Microsoft Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/overview) - for managing game state
* [next.js and React](https://nextjs.org) - SPA and Client
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

## "Backend" API Server

The server-side logic is written in C# and uses ASP.NET Core. i
(See the `ScaryCavesWeb` project.) This component also runs the Orleans Silo 
which manages various game state through Actors.

The server runs on port 8000 by default.

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

## Front-End Client and Single Page Application

The client side is written in TypeScript and uses next.js and React. See the `scarycaves-next` project.

### Running the Client
The client runs locally at http://localhost:3000 by default. You can run the client
from the command line with `npm run dev` from the `scarycaves-next` directory.

### Local Client Settings

Create a .env file that defines a few public settings: (note that these are 
visible to the client and therefore not secret):
```bash
NEXT_PUBLIC_RECAPTCHA_SITE_KEY=public_but_still_wont_tell_you_ha_ha
NEXT_PUBLIC_BASE_URL=http://localhost:3000
NEXT_PUBLIC_API_BASE_URL=http://localhost:8000
```
(See env.local for examples)

### ReCaptcha v3

The client uses Google's ReCaptcha v3 to help prevent bots from spamming the server
and being a nuisance. The secret key is stored ... secretly. ReCaptcha v3 doesn't
require any user interaction, and returns a score of humanness. (0.0 you are a robot,
1.0 you are a human.) 50% seemed like a good-enough value.

## Various Settings

See [ScaryCavesWeb/appsettings.json](ScaryCavesWeb/appsettings.json) for the 
settings that you might want to override. 

* `RedisConnectionString` - where to find the Redis database 
* `AccountTimeToLiveSeconds` - Since this is just for fun, accounts and players expire after this amount of time.
* `MobActivityTimer` - Controls how often a Mob wakes up and decides what to do. Lower value makes a more interesting demo, but requires more cpu.
* `DefaultZoneId` and `DefaultRoomId` - Where player start (or reset) to.
* `ReCaptcha` - the secret key should be loaded from an environment variable for production or from DotNet User Secrets for development. The threshold is how human one must be to pass. 
* `DataProtectionKeyPath` - where should the AspNet app write its Data Protection XML files? This should be a secure location.
* `DataProtectionCert` - See the section on Data Protection Keys and other Secrets above.

# Docker Swarm

See [build_images.sh](build_images.sh) for how to build the "production" images.
There are two Dockerfiles - one for `scary_aspnet` and one for `scary_next`. 
Remember that next.js "compiles in" PUBLIC_NEXT_ environment variables at build
time, which is weird, but whatever. 

After building, [push_images.sh](push_images.sh) will push the latest images to
Docker Hub (public repository).

See [stack.yml](stack.yml) for the Swarm Production configuration. That will also
show how to configure the secrets (and other settings) via environment variables.

To deploy the stack, use `docker stack deploy -c stack.yml scarycaves`. 
See [deploy_local_stack.sh](deploy_local_stack.sh) for an example.

# Nginx Configuration

The Nginx configuration is in [scarycaves](scarycaves). It's a pretty basic
https termination and proxy setup. 

Since I can't make up my mind if this is 'scary cave' or 'scary caves', we
forward both to the same place, then redirect the singular over to 'scarycaves'.
The TLS certificate works for both.

All traffic is routed to the next-js app, unless it is prefixed with /app
in which case it goes to the backend server. The /app is removed before aspnet
sees it.

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
