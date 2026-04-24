$(function () {
    var typeConfig = {
        "0": { tableName: "Engagements",           sectionId: "engagement-search-section" },
        "1": { tableName: "Talks",                 sectionId: "talks-search-section"      },
        "2": { tableName: "SyndicationFeedSources", sectionId: "feed-search-section"      },
        "3": { tableName: "YouTubeSources",         sectionId: "youtube-search-section"   }
    };

    function escapeHtml(str) {
        return $('<div>').text(str).html();
    }

    function showSearchPanel(typeValue) {
        $('.source-search-section').hide();
        if (typeValue && typeConfig[typeValue]) {
            $('#source-search-panel').show();
            $('#' + typeConfig[typeValue].sectionId).show();
        } else {
            $('#source-search-panel').hide();
        }
    }

    function showSelectedItem(name) {
        $('#selected-item-name').text(name);
        $('#selected-item-display').show();
        $('#source-search-panel').hide();
    }

    function resetSearch() {
        $('#ItemPrimaryKey').val('0');
        $('#selected-item-display').hide();
        $('#selected-item-name').text('');
        $('#engagement-results, #talks-engagement-results, #feed-results, #youtube-results').html('');
        $('#talks-select-panel').hide();
        $('#talks-select').html('<option value="">-- Select a Talk --</option>');
        showSearchPanel($('#ItemType').val());
    }

    function selectItem(id, name) {
        $('#ItemPrimaryKey').val(id);
        showSelectedItem(name);
    }

    function renderResults(results, containerId, onSelect) {
        var container = $('#' + containerId);
        if (!results || results.length === 0) {
            container.html('<div class="alert alert-warning mt-1 py-2 mb-0">No results found.</div>');
            return;
        }
        var html = '<ul class="list-group mt-1">';
        $.each(results, function (i, item) {
            html += '<li class="list-group-item list-group-item-action py-2" role="button"' +
                    ' data-id="' + item.id + '" data-name="' + escapeHtml(item.name) + '">' +
                    escapeHtml(item.name) + '</li>';
        });
        html += '</ul>';
        container.html(html);
        container.find('.list-group-item').on('click', function () {
            onSelect(parseInt($(this).data('id')), $(this).data('name'));
        });
    }

    function doSearch(url, params, containerId, onSelect) {
        var container = $('#' + containerId);
        container.html('<div class="text-muted mt-1"><span class="spinner-border spinner-border-sm me-1"></span>Searching...</div>');
        $.getJSON(url, params, function (results) {
            renderResults(results, containerId, onSelect);
        }).fail(function () {
            container.html('<div class="alert alert-danger mt-1 py-2 mb-0">Search failed. Please try again.</div>');
        });
    }

    // ── ItemType change ───────────────────────────────────────────────────────
    $('#ItemType').on('change', function () {
        var selectedType = $(this).val();
        var config = typeConfig[selectedType];
        $('#ItemTableName').val(config ? config.tableName : '');
        $('#ItemPrimaryKey').val('0');
        $('#selected-item-display').hide();
        $('#selected-item-name').text('');
        $('#engagement-results, #talks-engagement-results, #feed-results, #youtube-results').html('');
        $('#talks-select-panel').hide();
        $('#talks-select').html('<option value="">-- Select a Talk --</option>');
        showSearchPanel(selectedType);
    });

    // ── Engagement search ─────────────────────────────────────────────────────
    function searchEngagements() {
        doSearch('/Schedules/SearchEngagements', { q: $('#engagement-search-input').val().trim() },
            'engagement-results', function (id, name) { selectItem(id, name); });
    }
    $('#engagement-search-btn').on('click', searchEngagements);
    $('#engagement-search-input').on('keypress', function (e) {
        if (e.which === 13) { e.preventDefault(); searchEngagements(); }
    });

    // ── Talks: two-step (engagement → talk) ───────────────────────────────────
    function searchTalksEngagement() {
        doSearch('/Schedules/SearchEngagements', { q: $('#talks-engagement-input').val().trim() },
            'talks-engagement-results', function (engagementId) {
                $('#talks-select').html('<option value="">-- Select a Talk --</option>');
                $('#talks-select-panel').show();
                $.getJSON('/Schedules/GetTalksByEngagement', { engagementId: engagementId }, function (talks) {
                    if (!talks || talks.length === 0) {
                        $('#talks-select').html('<option value="">No talks found for this engagement</option>');
                        return;
                    }
                    var options = '<option value="">-- Select a Talk --</option>';
                    $.each(talks, function (i, talk) {
                        options += '<option value="' + talk.id + '" data-name="' + escapeHtml(talk.name) + '">' +
                                   escapeHtml(talk.name) + '</option>';
                    });
                    $('#talks-select').html(options);
                }).fail(function () {
                    $('#talks-select').html('<option value="">Failed to load talks</option>');
                });
            });
    }
    $('#talks-engagement-btn').on('click', searchTalksEngagement);
    $('#talks-engagement-input').on('keypress', function (e) {
        if (e.which === 13) { e.preventDefault(); searchTalksEngagement(); }
    });
    $('#talks-select').on('change', function () {
        var id = parseInt($(this).val());
        if (id > 0) {
            selectItem(id, $(this).find('option:selected').data('name'));
        }
    });

    // ── Feed source search ────────────────────────────────────────────────────
    function searchFeedSources() {
        doSearch('/Schedules/SearchSyndicationFeedSources', { q: $('#feed-search-input').val().trim() },
            'feed-results', function (id, name) { selectItem(id, name); });
    }
    $('#feed-search-btn').on('click', searchFeedSources);
    $('#feed-search-input').on('keypress', function (e) {
        if (e.which === 13) { e.preventDefault(); searchFeedSources(); }
    });

    // ── YouTube source search ─────────────────────────────────────────────────
    function searchYouTubeSources() {
        doSearch('/Schedules/SearchYouTubeSources', { q: $('#youtube-search-input').val().trim() },
            'youtube-results', function (id, name) { selectItem(id, name); });
    }
    $('#youtube-search-btn').on('click', searchYouTubeSources);
    $('#youtube-search-input').on('keypress', function (e) {
        if (e.which === 13) { e.preventDefault(); searchYouTubeSources(); }
    });

    // ── Clear / Change button ─────────────────────────────────────────────────
    $('#clear-item-btn').on('click', resetSearch);

    // ── Page-load pre-population (Edit form) ─────────────────────────────────
    var initialPrimaryKey = parseInt($('#ItemPrimaryKey').val() || '0');
    var initialItemType   = $('#ItemType').val();

    if (initialItemType && typeConfig[initialItemType]) {
        $('#ItemTableName').val(typeConfig[initialItemType].tableName);
    }

    if (initialPrimaryKey > 0 && initialItemType) {
        $('#selected-item-display').show();
        $('#selected-item-name').text('Loading...');
        $.getJSON('/Schedules/ValidateItem', { itemType: initialItemType, itemPrimaryKey: initialPrimaryKey },
            function (result) {
                if (result.isValid) {
                    showSelectedItem(result.itemTitle);
                } else {
                    $('#selected-item-display').hide();
                    showSearchPanel(initialItemType);
                }
            }).fail(function () {
                $('#selected-item-display').hide();
                showSearchPanel(initialItemType);
            });
    } else if (initialItemType) {
        showSearchPanel(initialItemType);
    }
});
