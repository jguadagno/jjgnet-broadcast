# JosephGuadagno.NET Broadcasting

The name is still subject to *change* :smile:

![JosephGuadagno.NET Broadcasting Image](./imgs/jjgnet-broadcast-hero.png)

Repository for the automated broadcasting of blog posts, conferences, and streams. The goal behind the project is to provide a way to *broadcast* out information from one or more sources (collectors) and send them to other sources (publishers).  This is not a data synchronization framework, but a replacement for services like Hoot Suite, IFTTT, etc. Currently, the function can monitor an RSS Feed and a YouTube playlist for new content.  Once new content is found, it will send information about Twitter and Facebook.  In other words, once I create a new blog post, this project will tweet about it and post it to my Facebook wall.

## Plans

The next thing I want to add is the ability to store my upcoming speaking engagements, whether they be at a conference or the stream. Then, this project will have a way to '*schedule*' notifications/social broadcasting of the events.

### Example

Say I am speaking a conference in one month, I will be able to announce '*Hey, I'm speaking at ..., Details at:...*', I could schedule broadcast in 2 weeks, '*Sign up for my talk at ...*', then 30 minutes before the talk '*I'm up next in room ..., come watch*', then after the talk '*Hey, the content for my talk is available at ...*'  

## Infrastructure

The infrastructure requirements are documented in [infrastructure](infrastructure.md). Currently, there are no scripts to automatically create the infrastructure and deploy the functions. This was all done manually. In a future release, I plan to have Aspire handle the deployments, but for now, I am focusing on adding features.

Most of the settings are stored in user secrets and not committed to the repository.  I've kept the [local.settings.json](src/JosephGuadagno.Broadcasting.Functions/local.settings.json) file up to date with the settings that are required.  You'll have to fill in the blanks. 
