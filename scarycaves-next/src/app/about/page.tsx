"use client";

const AboutPage = () => {
    return (
        <div>
            <h2 className="display-6">About the Scary Cave</h2>
            <p>This is a simple text-based adventure game. It is a work in progress!</p>
            <p>
                 It was created to demonstrate some networking basics and to be some example code for the Computer Networks I & II classes (CS-260 and CS-261).
            </p>
            <p>
                As such, it is not a complete game and may have some bugs or other issues; and is not intended to be
                a complete and polished product.
            </p>
            <br/>
            <h4 className="display-6">Interesting Technology:</h4>
            <ul className="tech-list">
                <li><a href="https://github.com/DigiPen-Network-Classes/ScaryCaves">This Project (Github)</a></li>
                <li><a href="https://dotnet.microsoft.com/en-us/apps/aspnet">Asp.Net Core</a> for the Server API</li>
                <li><a href="https://learn.microsoft.com/en-us/dotnet/orleans/overview">Microsoft Orleans</a> - for managing game state</li>
                <li><a href="https://nextjs.org">next.js and React</a> - SPA and Client</li>
                <li><a href="https://redis.io">Redis</a> - Game Database</li>
            </ul>
            <br/>
        </div>
    );
};
export default AboutPage;
