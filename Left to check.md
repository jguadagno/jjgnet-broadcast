# Left to check

## Web UI

- [ ] Engagements - CRUD
- [ ] Schedules - CRUD
- [X] Admin - Message Templates - These should be unique per user and work in the Functions
- [X] Admin - Syndication Feed Sources - These should be unique per user and work in the Functions
- [X] Admin - YouTube Sources - These should be unique per user and work in the Functions
- [ ] Site Admin - Social Media Platforms - CRUD
- [X] Site Admin - Users
- [X] LinkedIn - validate the token rotation works
- [X] Update the page description - TODOs
- [ ] Synchronize application settings in App with local, production, and settings class
- [X] Fix the broken links to the Scriban docs on MessageTemplates/Edit

## API

- [X] Synchronize application settings in App with local, production, and settings class

## Database

- [ ] Make sure local and prod are the same
- [ ] Make sure the `table-create` scripts are accurate
- [ ] Make sure the `data-seed` scripts are accurate
- [ ] Get my "secrets" for publishers and collectors from configuration/key vault into the database
  - [ ] YouTube
  - [ ] LinkedIn
  - [ ] Facebook
  - [ ] Bluesky
  - [ ] Twitter/X

## Functions

- [ ] Token Refresh - Facebook
- [ ] Notification of Expiring LinkedIn Token - 7 day
  - [ ] It runs
  - [ ] Email is sent out

- [ ] Notification of Expiring LinkedIn Token - 1 day
  - [ ] It runs
  - [ ] Email is sent out

- [ ] Remove the "temporary" setting of `OwnerEntraOid`
- [ ] Synchronize application settings in App with local, production, and settings class

- [X] LoadAllPosts, needs a parameter added for EntraOid
- [X] LoadAllVideos, needs a parameter added for EntraOid

### Validate Functions for posting - Bluesky

- [ ] Random Post
- [ ] Speaking Engagement
- [ ] Syndication Item
- [ ] YouTube
- [ ] Scheduled Item

### Validate Functions for posting - Facebook

- [ ] Random Post
- [ ] Speaking Engagement
- [ ] Syndication Item
- [ ] YouTube
- [ ] Scheduled Item

### Validate Functions for posting - LinkedIn

- [ ] Random Post
- [ ] Speaking Engagement
- [ ] Syndication Item
- [ ] YouTube
- [ ] Scheduled Item

### Validate Functions for posting - Twitter/X

- [ ] Random Post
- [ ] Speaking Engagement
- [ ] Syndication Item
- [ ] YouTube
- [ ] Scheduled Item
            
## Miscellaneous

- [] Register Health Checks with Aspire
  - [ ] Functions
  - [ ] Web
  - [ ] API

- [ ] Validate all KeyVault secrets, remove ones that are no longer needed

## Message Templates

- [ ] Make sure the message templates are correct

  - [X] Make sure there is a public model for each type of message
  - [X] One message template in the database for each message used in code
  - [ ] Update the `data-seed.sql` with my corrected version
  - [X] Each message template is unique to the EntraOId

- [ ] Delete the MessageTemplates folder in the Functions app

### Questions

- [ ] Do we need to create a message template editing page per user/type of message. Validate with the Web and code.
- [X] How are the message types stored, loaded and displayed?
- [ ] Do we need a nice editor for it?  Should we display the model that is passed to it.

