# The Scary Cave -- an HTTP based Text Adventure

HTTP is stateless. Let's build a (very!) simple text game with multiple users and state,
using HTTP to make all the pieces go and using a Single Page Application as a client.

## About the Scary Cave

This is a simple text-based adventure game. It is a work in progress!
It was created to demonstrate some networking basics and to be some example code for the Computer Networks I & II classes (CS-260 and CS-261).
As such, it is not a complete game and may have some bugs or other issues; and is not intended to be a "complete and polished product."

## Interesting Technology

* [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) - for the "Server API"
* [Microsoft Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/overview) - for managing game state
* [next.js and React](https://nextjs.org) - SPA and Client
* [Redis](https://redis.io) - Game State Database
* [MongoDB](https://mongodb.com) - Accounts and Players Database
