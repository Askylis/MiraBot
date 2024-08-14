## Introduction

MiraBot is a Discord bot! She has a variety of commands that can be used in private messages and in servers, and has access to some simple tools that I've built. Her tools right now are very simple and largely intended as a way for me to practice and learn. 

## Why did I make Mira?

I wanted a project to learn more about and practice concepts that I wasn't as familiar with as I'd like to be. These concepts include: asynchronous code, databases, Entity Framework, and dependency injection. Making a Discord bot was a fantastic way to practice all of those things at once. Throughout my time making Mira, I became much more comfortable with those topics, and have gained a lot of valuable experience. 

## What can Mira do? 

Not much right now. She currently has one completed module, and one in-progress module. The completed module is a very oversized solution to a very basic problem. It's called "GroceryAssistant", and it's a tool that I originally wrote as a console application when I was first learning C#, and I completely re-wrote for use in Mira. You can use it to store meals and their ingredients, and it'll randomly select a user-defined number of your saved meals. I made this because ordering groceries is stressful and time consuming. Deciding what to eat every week, and trying to remember all the ingredients for everything we eat was an annoying problem, so I aimed to store that information and automate meal selection so that minor stress is removed from my life.

Her in-progress module is called Miraminders. It's a (currently extremely basic) reminders application that can store and send off reminders for yourself, and you can also have reminders sent to any other user who has previously interacted with Mira. These reminders go off at a time determined by however long from the current time that the user requests. Miraminders will be updated to handle much more complex reminders, including recurring reminders and reminders that happen at a specific, user-requested time.

## What will you add to Mira?
I plan to continue to update Miraminders to make it able to handle a wide variety of reminders, instead of the basic ones it's currently capable of. I plan to add some general use commands to her too (including bug reporting, a help command, and setting a nickname for yourself). Additionally, I plan on building some server management and moderation tools for Mira to help with server administration. 

## How do I use Mira?
Mira takes advantage of Discord's slash commands. In a private chat with her (or in a server she's in), you can enter a `/` and see a list of all available commands. She has a suite of commands available for GroceryAssistant (including `/listmeals`, `/addmeal`, `/editmeal`, `/deletemeal`, and the main one, `/ga` which handles the process of actually selecting a user-defined number of random meals and presenting them to the user, along with their associated ingredients).

Miraminders is pretty barebones for now, and only features a simple `/remind` command. I'll add more options in the future, including options for recurring reminders. 

## Why do you keep referring to Mira as "she"?
Mira is the name of my server that I've owned for many years! And now, this Discord bot is named after her. While primarily a file server, she also hosts game servers for my friends and myself, and now she also hosts the Mira Discord bot! I've become very attached to my server, and she is very special to me. 
