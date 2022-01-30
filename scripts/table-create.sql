
create table dbo.Engagements
(
    Id int identity
        constraint Engagements_pk
            primary key nonclustered,
    Name nvarchar(max) not null,
    Url nvarchar(max),
    StartDateTime Datetimeoffset not null,
    EndDateTime datetimeoffset not null,
    Comments nvarchar(max)
)
go

create table dbo.Talks
(
    Id int identity
        constraint Talks_pk
            primary key nonclustered,
    EngagementId int
        constraint Talks_Engagements_Id
            references Engagements (Id),
    Name nvarchar(max) not null,
    UrlForConferenceTalk nvarchar(max),
    UrlForTalk nvarchar(max),
    StartDateTime datetimeoffset not null,
    EndDateTime datetimeoffset not null,
    TalkLocation nvarchar(max),
    Comments nvarchar(max)
)
go


create table dbo.ScheduledItems
(
    Id int identity
        constraint ScheduledItems_pk
            primary key nonclustered,
    ItemTable varchar(255) not null,
    ItemPrimaryKey varchar(255) not null,
    Message nvarchar(max),
    SendOnDateTime datetimeoffset not null,
    MessageSent bit default 0 not null,
    MessageSentOn datetimeoffset
)
go
