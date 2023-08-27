# JosephGuadagno.NET Broadcasting

The name is still subject to *change* :smile:

The user interface for this project is being built at [jguadagno/MyEventPresentations](https://github.com/jguadagno/MyEventPresentations)

Repository for the automated broadcasting of blog post, conferences, and streams. The goal behind the project is to provide a way to *broadcast* out information from one or more sources (collectors) and send them to other sources (publishers).  This is not a data synchronization framework, but a replacement for services like Hoot Suite, IFTTT, etc. Currently, the function can monitor an RSS Feed and a YouTube playlist for new content.  Once new content is found, it will send information about Twitter and Facebook.  In other words, once I create a new blog post, this project will tweet about it and post it to my Facebook wall.

## Future Plans

The next thing I want to add is the ability to store my upcoming speaking engagements, whether they be at a conference or the stream. The user interface will be handled with [jguadagno/MyEventPresentations](https://github.com/jguadagno/MyEventPresentations).  This project will have a way to '*schedule*' notifications/social broadcasting of the events.

Say I am speaking a conference in one month, I will be able to announce '*Hey, I'm speaking at ..., Details at:...*', I could schedule broadcast in 2 weeks, '*Sign up for my talk at ..*', then 30 minutes before the talk '*I'm up next in room ..., come watch*', then after the talk '*Hey, the content for my talk is available at ...*'  

## Infrastructure Needs

The infrastructure requirements are documented in [infrastructure-needs](infrastructure-needs.md). Currently, there are no scripts to automatically create the infrastructure.  There is an [issue](https://github.com/jguadagno/jjgnet-broadcast/issues/16) to build the script.

Most of the settings are stored in users secrets and not committed to the repository.  I've kept the [local.settings.json](src/JosephGuadagno.Broadcasting.Functions/local.settings.json) file update to date with the settings that are required.  You'll just have to fill in the blanks. 
