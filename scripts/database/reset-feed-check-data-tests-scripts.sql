SELECT * FROM FeedChecks

-- Reset New Posts
DELETE FROM SyndicationFeedSources WHERE Url = 'http://localhost:4000/2026/02/16/jetbrains-rider-settings-for-presentations'
UPDATE FeedChecks SET LastCheckedFeed = '2026-02-17 12:00:00 -07:00', LastItemAddedOrUpdated= '2026-01-10 13:28:00.0000000 +00:00' WHERE Id = 1 -- New Post

SELECT * FROM SyndicationFeedSources WHERE Url like '%http://localhost:4000/%'

-- Reset Speaking Engagements
DELETE FROM Engagements WHERE Name='Greater Lansing User Group for .NET Developers' AND [Url]='https://www.meetup.com/glugnet/events/308564233/' AND StartDateTime='2026-02-19 18:00:00.0000000 -05:00'
DELETE FROM Engagements WHERE Name='CodeMash' AND Url='https://www.codemash.org/' AND StartDateTime='2026-01-13 09:00:00.0000000 -05:00'
UPDATE FeedChecks SET LastCheckedFeed = '2026-02-02 12:00:00 -07:00', LastItemAddedOrUpdated='2026-02-02 12:00:00 -07:00' WHERE Id = 2 -- Speaking Engagements

SELECT * FROM Engagements WHERE StartDateTime > '2026-01-01'

-- Reset YouTube Checks
DELETE FROM YouTubeSources WHERE VideoId = 'N1p6WDFIVFRUVTQ='
UPDATE FeedChecks SET LastCheckedFeed = '2025-06-12 12:00:00 -07:00', LastItemAddedOrUpdated='2025-06-12 12:00:00 -07:00' WHERE Id = 3 -- New Videos

SELECT * FROM YouTubeSources WHERE VideoId = 'N1p6WDFIVFRUVTQ='
