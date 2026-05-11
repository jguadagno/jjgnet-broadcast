using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader;

public class SpeakingEngagementsReader: ISpeakingEngagementsReader
{
    private readonly HttpClient _httpClient;
    private readonly ISpeakingEngagementsReaderSettings _settings;
    private readonly ILogger<SpeakingEngagementsReader> _logger;

    public SpeakingEngagementsReader(HttpClient httpClient, ISpeakingEngagementsReaderSettings? settings, ILogger<SpeakingEngagementsReader> logger)
    {

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings), "The SpeakingEngagementsReaderSettings cannot be null");
        }

        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<List<Engagement>> GetAll(DateTimeOffset sinceWhen)
    {
        if (string.IsNullOrEmpty(_settings.SpeakingEngagementsFile))
        {
            throw new InvalidOperationException("SpeakingEngagementsFile is not configured. Use GetAll(string fileUrl, DateTimeOffset sinceWhen) for per-user file URLs.");
        }

        var speakingEngagements = await LoadAllSpeakingEngagementsFromUrl(_settings.SpeakingEngagementsFile);
        return speakingEngagements.Where(e => e.LastUpdatedOn > sinceWhen).ToList();
    }

    public async Task<List<Engagement>> GetAll()
    {
        if (string.IsNullOrEmpty(_settings.SpeakingEngagementsFile))
        {
            throw new InvalidOperationException("SpeakingEngagementsFile is not configured. Use GetAll(string fileUrl) for per-user file URLs.");
        }

        return await LoadAllSpeakingEngagementsFromUrl(_settings.SpeakingEngagementsFile);
    }

    public async Task<List<Engagement>> GetAll(string fileUrl, DateTimeOffset sinceWhen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUrl);

        var speakingEngagements = await LoadAllSpeakingEngagementsFromUrl(fileUrl);
        return speakingEngagements.Where(e => e.LastUpdatedOn > sinceWhen).ToList();
    }

    public async Task<List<Engagement>> GetAll(string fileUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUrl);

        return await LoadAllSpeakingEngagementsFromUrl(fileUrl);
    }

    private async Task<List<Engagement>> LoadAllSpeakingEngagementsFromUrl(string fileUrl)
    {
        var engagements = new List<Engagement>();

        _logger.LogDebug("Reading all the speaking engagements from '{Url}'", fileUrl);

        try
        {
            // Load the data
            List<Models.Engagement>? speakingEngagements =
                await _httpClient.GetFromJsonAsync<List<Models.Engagement>>(fileUrl);

            if (speakingEngagements is null)
            {
                return engagements;
            }

            // Transform the data
            foreach (var speakingEngagement in speakingEngagements)
            {
                var engagement = new Engagement
                {
                    Name = speakingEngagement.EventName!,
                    Url = speakingEngagement.EventUrl!,
                    StartDateTime = speakingEngagement.EventStart,
                    EndDateTime = speakingEngagement.EventEnd,
                    TimeZoneId = speakingEngagement.Timezone!,
                    Comments =  speakingEngagement.Comments,
                    CreatedOn = speakingEngagement.CreatedOrUpdatedOn,
                    LastUpdatedOn = speakingEngagement.CreatedOrUpdatedOn
                };
                if (speakingEngagement.Presentations.Count != 0)
                {
                    engagement.Talks = [];

                    foreach (var talk in speakingEngagement.Presentations)
                    {
                        engagement.Talks.Add(new Talk
                        {
                            Name = talk.Name!,
                            UrlForTalk = talk.Url!,
                            UrlForConferenceTalk =  talk.Url!,
                            StartDateTime = talk.PresentationStartDateTime ?? speakingEngagement.EventStart,
                            EndDateTime = talk.PresentationEndDateTime ?? speakingEngagement.EventEnd,
                            TalkLocation = talk.Room,
                            Comments = talk.Comments
                        });
                    }
                }
                engagements.Add(engagement);
            }

            // Return the new collection
            _logger.LogDebug("Read {Count} all the speaking engagements from '{Url}'", engagements.Count, fileUrl);
            return engagements;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load all the speaking engagements from '{Url}'", fileUrl);
            return engagements;
        }
    }
}