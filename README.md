# MiraBot ![Unit Tests Status](https://github.com/Askylis/MiraBot/actions/workflows/unit-tests.yml/badge.svg)

## Introduction

MiraBot is a Discord bot! She has a variety of commands that can be used in private messages and in servers, and has access to a variety of tools that I've built. Her tools are largely intended as a way for me to practice and learn. 

## Why did I make Mira?

I wanted a project to learn more about and practice concepts that I wasn't as familiar with as I'd like to be. These concepts include: asynchronous code, databases, Entity Framework, and dependency injection. Making a Discord bot was a fantastic way to practice all of those things at once. Throughout my time making Mira, I became much more comfortable with those topics, and have gained a lot of valuable experience. 

## What can Mira do? 

She's a utility bot, and can help tackle some day to day problems! She currently has four completed modules, with more on the way! 

### First module:
The first completed module is a very oversized solution to a very basic problem. It's called "GroceryAssistant", and it's a tool that I originally wrote as a console application when I was first learning C#, and I completely re-wrote for use in Mira. It's a tool to help me and my family plan what we're going to eat each week, and which groceries to buy in order to make whatever we have planned! Additionally, GroceryAssistant can be used to save recipes alongside the meals, and you can use GroceryAssistant to share meals and recipes with other users.

### Second module:
Her second module is called Miraminders. It's a reminders application that can store and send off reminders for yourself, and you can also have reminders sent to any other user who has previously interacted with Mira. These reminders go off at a time and date specified by the user. Miraminders can also handle more complex recurring reminders. Mira processes reminders with the ReminderHandler class, which analyzes natural speech patterns that are given to her, rather than using a series of required parameters. What this means is that you can simply input something like: "`/remind me every 5 days that I need to complete this task`", rather than having to input `/remind` and tab through a bunch of different parameters. This also simplifies her commands, as there are so many different ways to handle reminders that I would need several different `/remind` commands to handle them all. For example, she would need separate commands for the following reminder types (keep in mind that this list is not exhaustive):

  - One off reminders that go off in a certain amount of time from now (`remind me in 15 minutes to do this task`).
  - One off reminders that go off at a specified time (`remind me at 10:15 PM to complete this thing`).
  - Recurring reminders that go off at specific intervals (`remind me every 15 minutes to do this thing`).
  - Recurring reminders that go off at specific times (`remind me every day at 9:30 AM to do this thing`).
  - Recurring reminders that go off on specific dates (`remind me on the 13th of every month that I need to go to this place`).

Parsing natural language instead consolidates all her logic into a single command, instead of requiring a separate command for each of those examples. Since ReminderHandler is intended to parse natural language, it includes many quality of life features to make it as pleasant to use as possible. It is flexible in how it can understand times and dates. This means that it can handle different date formats, such as dd/mm and mm/dd, and it can handle dates that are typed out with names (e.g. `August 13th`), or typed out numerically (e.g. `8/13`). It can handle times similarly, allowing you to use either 12 hour time or 24 hour time (`9:14 pm`, `9:14PM`, and `21:24` are all valid time inputs). ReminderHandler will automatically detect the user's intent and build the correct reminder, including adding the intended recipient and intended message attached to the reminder. 

### Third module:
Her third module is an administrative module that handles permissions, as well as banning and unbanning users from interacting with Mira. The administrative module uses text-based commands instead of Discord's slash commands, so that users can't see these commands. These commands have permissions on them that prevent anyone except for me from using them. The administrative module also implements custom Precondition Attributes, which I use for all commands, to make sure that people can't just use any command they'd like right away. For example, my administrative module uses my `RequiresCustomPermissionAttribute` to check and see if the person trying to use the command has custom permission `Owner`. Currently, I am the only person with that permission. This attribute can also check for any other permission that I've created. The rest of her commands only use `NotBannedAttribute` currently to make sure that blacklisted people can't use any of her commands or services. 

### Fourth module:
Her fourth module is a general use module, that includes `/register` so new users can register with her, which just involves collecting basic information about them so they can use her various functions. It also includes a `/help` command, which lists all of her available commands, along with a description of each command. There is also a `/bugreport` command, which lets users report a bug and include information such as bug severity, a description of the bug, how to reproduce it, and sends that information, along with the date and time it was reported and information about the user, to me so I can analyze and fix it. The administrative module includes commands that I can use to view and manage bugs, including listing them all, seeing information about the bugs, and marking them as fixed. 

Mira is very much a work in progress and is nowhere near done. I plan to keep adding features to her until I'm content with what she's capable of. 

## What will you add to Mira?
I plan to add "consent" functionality. Since Mira can be used to message other users (either by sending them reminders, or sharing recipes), I plan to add blacklists and whitelists for each user. When a user receives a message through Mira from another user for the first time, instead of just sending the message, Mira will instead inform the recipient that `username` is trying to send them a `reminder`/`recipe`, and give the user the option to either allow or block that user. User blacklist and whitelist preferences will be able to be updated at any time with appropriate `/blacklist` and `/whitelist` commands. Additionally, I plan on building some server management and moderation tools for Mira to help with server administration.

## How do I use Mira?
Mira takes advantage of Discord's slash commands. In a private chat with her (or in a server she's in), you can enter a `/` and see a list of all available commands. New users will need to use `/register` before being able to access her functionality, since she'll need to gather some basic information about the user that's required for some of her functionality to work. Users can use `/help` to view all available commands, as well as a description of each command's functionality. 

She has a suite of commands available for GroceryAssistant (including `/listmeals`, `/addmeal`, `/editmeal`, `/deletemeal`, and the main one, `/ga` which handles the process of actually selecting a user-defined number of random meals and presenting them to the user, along with their associated ingredients).

Miraminders's main command is `/remind`, which allows you to configure custom reminders. Miraminders also contains `/remindcancel`, `/remindlist`, and `/remindfind`. 

Users can submit bug reports using `/bugreport`. 

## Why do you keep referring to Mira as "she"?
Mira is the name of my server that I've owned for many years! And now, this Discord bot is named after her. While primarily a file server, she also hosts game servers for my friends and myself, and now she also hosts the Mira Discord bot! I've become very attached to my server, and she is very special to me. 
