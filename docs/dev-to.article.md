## Why?

If you live in a terminal, skip this one — it won't do much for you.

But if your day starts in Windows Explorer, you already know the gap I'm talking about: browsing files is easy, doing anything with them is not.

You open a folder, poke around, jump between project directories, right-click, copy a path, open a terminal, paste the path, launch an editor, repeat. None of it is hard. It's just a lot of tiny rituals, done dozens of times a day, and they add up to a surprising amount of drag.

Explobar exists to cut that drag out.

## The Windows Explorer gap

For a lot of Windows developers, Explorer is the default workspace — not for everything, but for plenty of real work:

- opening a repo you just cloned
- browsing generated output
- digging through logs, build artifacts, config files
- comparing folders
- getting to the right directory before launching a tool

I think of this as a *gemba* habit: you go to where the work physically lives — the file system — and act from there.

Explorer isn't bad at browsing. It's bad at acting. Open a terminal. Copy the path. Paste it. Launch the editor. Run the helper script. Open properties. Create a file. Create a folder. Find your way back to a folder you were just in an hour ago.

None of that is fluid.

### A note on QTTabBar

QTTabBar deserves a mention here. For years it was the thing that made Explorer actually usable if you were a power user — it showed what Explorer could be with the right extensions bolted on.

But the extension approach hasn't aged well. Windows 10 and 11 changed how Explorer hosts add-ins, and that quietly broke the assumptions QTTabBar (and tools like it) depended on. A great extension point turned into an ongoing maintenance headache.

None of this is a knock on QTTabBar — it was, and still is, excellent. It's just that Windows moved the ground underneath it.

### If you already live in a terminal, you don't need this

Shell users already have this solved: fast navigation, aliases, scripts, history, fuzzy finders, editor integration — the whole workflow leans on text, and terminals are great at text. Explobar isn't trying to compete with that.

It's for people who work on Windows and think visually first — Explorer is where they orient themselves — and who then want fast access to the tools and commands relevant to whatever they're looking at.

## What Explobar actually does

Explobar drops a customizable floating toolbar onto Explorer. Hit a hotkey and it pops up right where you're working, already aware of the current folder and whatever you've got selected. From there you launch apps, run custom commands, jump to recent locations, trigger file actions, or hook in your own scripts.

![Explobar in action](https://raw.githubusercontent.com/oleg-shilo/explobar/main/docs/explobar.gif)

It overlaps a bit with Explorer's right-click menu — both give you contextual actions for whatever's in front of you. But it's faster, mainly because it isn't fighting Explorer's own menu, which every third-party app on your machine has been cramming entries into for a decade. You get a toolbar you built, with exactly what you want on it — nothing to dig through.

You decide what's on it. Three buttons, or thirty. Your call.

The friction it removes looks like this:

- copying a path
- alt-tabbing to a terminal
- retyping the same command
- hunting through a bloated context menu

And replaces it with: navigate, select, click.

That's really the whole pitch — shrink the gap between seeing something and acting on it.

One more thing, and it matters more than it sounds: Explobar runs as its own standalone process, outside Explorer. It's not a shell extension wedged into Explorer's internals. So if Microsoft changes something under the hood again, Explobar mostly doesn't care — unlike tools that live inside the process itself.

## Why I think this is worth your attention

The concept is simple, but it opens up more customization than you'd expect.

At its core, the toolbar is just buttons and actions, defined by you in a config file.

Start small: a YAML file with buttons for Terminal, Notepad, a couple of recent folders. That alone kills most of the friction.

Want to go further — say, computing hashes for every file you've got selected? Drop in a small .NET assembly, or even a single `.cs` file, and wire in whatever logic you need.

So it's:

- easy to start with
- quick to adapt
- as deep as you want to take it

## Who this is for

Explobar makes sense if:

- you develop on Windows
- Explorer is part of your daily routine, not an occasional detour
- you regularly launch tools from a folder or a selection of files
- switching between "looking at files" and "doing something with them" bugs you

If your day starts and ends in PowerShell, Bash, or Windows Terminal, you probably won't get much out of it.

If it starts in Explorer, though, this was built for you.

## Final thought

Most developer tooling chases editors, terminals, cloud workflows — makes sense, that's where a lot of the work is. But plenty of Windows developers still spend real time in the file system, and that side of the day gets almost no attention from tool builders.

Explobar is aimed squarely at that gap. It's not trying to replace your terminal or your editor — just to strip the friction out of the place a lot of Windows workflows still start: Explorer.

If that sounds like your setup, the project's on GitHub. Questions, suggestions, bug reports — drop them there: [https://github.com/oleg-shilo/explobar](https://github.com/oleg-shilo/explobar), or right here in the comments.

Installing it on Windows is one line:

```powershell
winget install explobar
```
