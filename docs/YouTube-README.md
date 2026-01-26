# JosephGuadagno.NET Broadcasting YouTube Credentials

To collect data from and monitor a YouTube playlist three resources are required:

1. API Key (Google Cloud Platform / YouTube Data API v3)
2. YouTube Channel ID
3. YouTube Playlist ID

The documentation below covers obtaining these resources.

## API Key

1. Begin by navigating to the [Google Cloud Platform](https://console.cloud.google.com/). You will then need to sign in to a Google account to access the Google Cloud Platform, and it's associated features.
2. Once you have navigated to the Google Cloud Platform proceed to type `YouTube` into the *Search products and resources* search bar near the top middle of the home/dashboard.
3. Within the search results you will want to find and click on YouTube Data API v3 to navigate to the needed YouTube API.

   ![YouTube Search And Results](./img/GoogleCloudPlatform_YouTubeAPISearch.png)
4. Once you have navigated to the YouTube Data API v3 you will be at an overview screen that will feature an *ENABLE* and *TRY THIS API* buttons. You will want to proceed by clicking the *ENABLE* button.
   ![Enable YouTube API](./img/GoogleCloudPlatform_YouTubeAPIEnable.png)
5. After clicking *ENABLE* you will be redirected to the YouTube Data API v3 overview page. On the left side there will be a list of menu items. You will want to navigate to the credential manager by clicking on *Credentials*.
   ![YouTube API Credential Manager](./img/GoogleCloudPlatform_YouTubeAPICredentialManager.png)
6. Once in the credential manager you will want to click on the *Create Credentials* button near the top of the Credentials window.
   ![YouTube Create Credentials](./img/GoogleCloudPlatform_YouTubeAPICreateCredentials.png)
7. After clicking *Create Credentials* you will be given a list of options to choose from. Select *API Key*.
   ![YouTube Select API Key Credential](./img/GoogleCloudPlatform_YouTubeAPICreateAPIKey.png)
8. Once you have selected *API Key* a new API Key will be generated. **Make note of this API Key as this is API Key resource you need.**
   ![YouTube API Key Example](./img/GoogleCloudPlatform_YouTubeAPIKeyPlaceholder.png)

**NOTE**: The API Key featured below is invalid and is only for demonstration purposes.

**NOTE**: You can return to the YouTube Data API v3 dashboard to refer back to any generated API Keys or to modify API Key names and restrictions.

## YouTube Channel ID

We will obtain the YouTube Channel ID through the YouTube Data API v3 "Try this API". This will avoid the need of using third party programs such as Postman and will avoid introducing unneeded complexity by trying to send requests through Postman.

1. Begin by navigating to the [Channels: list](https://developers.google.com/youtube/v3/docs/channels/list) section of the YouTube Data API v3.
2. Once on the Channels: list page a slide out section titled *Try this API* will appear on the right side of the screen. For ease of use click on the small box on top of the slide out. This will expand the *Try this API* to nearly full screen.

   ![YouTube API Channels List](./img/YouTubeAPI_ChannelsList.png)

   ![YouTube API Channels List Expand Button](./img/YouTubeAPI_ExpandButtonClick.png)
3. Once expanded you will be greeted with a plethora of different input fields. There are only two we need to be concerned with, `part` and `forUsername`. The first input field labeled `part` you will input `id` into. This is very important as this is what gets the Channel ID that you need.

   ![YouTube API Part ID](./img/YouTubeAPI_IDPart.png)
4. After you have entered `id` into the `part` field you will want to enter `snippet` into the `part` field by clicking the *+ (plus)* button next to where you entered `id`. This will specify that the request will return more details about the channel(s) that get returned.

   ![YouTube API Part Add](./img/YouTubeAPI_PartAdd.png)

   ![YouTube API Snippet Part](./img/YouTubeAPI_Snippetpart.png)
5. After you have entered `id` into the `part` field and `snippet` into the `part` field you will next want to enter the username of the Channel ID you want/need. In this example we will search for the channel `computerphile`. In the `forUsername` field enter the YouTube Channel you want to search for. You should now have `id` in the `part` field, `snippet` in the `part` field, and the channel name in the `forUsername` field. Your request is ready! The final request parameters should look like the following:

   ![YouTube API Final Request Parameters](./img/YouTubeAPI_FinalRequestParams.png)
6. The next step is executing and sending the request to the API. Scroll to the bottom of the *Try this API* modal and uncheck the Google OAuth 2.0 box. Make sure that API Key is checked, and you're ready to execute the request. Click on the *EXECUTE* button.

   ![YouTube API Execute Request](./img/YouTubeAPI_ExecuteRequest.png)
7. You just sent the request and the response will open in a small window in the bottom right with the HTTP status code and the application/json response. For our example with `computerphile` the request returned `totalResults` of 1 with the `id` of `UC9-y-6csu5WGm29I7JiwpnA`. Look at the highlighted portion in the example screenshot to find the Channel ID. This is the Channel ID for the `computerphile` YouTube channel.
Under the `snippet` section of the response you can find further details about the return channel(s) such as `title` and `description`. Use the details in the `snippet` section to confirm the correct channel. **Make note of the `id` as this is the Channel ID resource you need.**

   ![YouTube API Request Response](./img/YouTubeAPI_RequestResponse.png)

**NOTE**: This is important to remember. If you are searching for a channel with a partial username or a channel that may have other channels with a similar username you may return more than one result. Be sure to use the `snippet` section details to confirm the channel you are searching for. You can minimize returned results by entering the exact username into the `forUsername` field.

## YouTube Playlist ID

We will obtain the YouTube Playlist ID through the YouTube Data API v3 *Try this API*. This request is almost identical to the same request we made to get the Channel ID.

1. Begin by navigating to the [Playlists: list](https://developers.google.com/youtube/v3/docs/playlists/list) section of the YouTube Data API v3.
2. Once on the Playlists: list page a slide out section titled *Try this API* will appear on the right side of the screen. For ease of use click on the small box on top of the slide out. This will expand the *Try this API* to nearly full screen.

   ![YouTube API Channels List](./img/YouTubeAPI_PlaylistsList.png)

   ![YouTube API Channels List Expand Button](./img/YouTubeAPI_ExpandButtonClick.png)
3. Once expanded you will be greeted with a plethora of different input fields. There are only two we need to be concerned with, `part` and `channelId`. The first input field labeled `part` you will input `id`` into. This is very important as this is what gets the Playlist ID that you need.

   ![YouTube API Part ID](./img/YouTubeAPI_IDPart.png)
4. After you have entered `id` into the `part` field you will want to enter `snippet` into the `part` field by clicking the *+ (plus)* button next to where you entered `id`. This will specify that the request will return more details about the playlist(s) that get returned.
   ![YouTube API Part Add](./img/YouTubeAPI_PartAdd.png)

   ![YouTube API Snippet Part](./img/YouTubeAPI_Snippetpart.png)
5. After you have entered `id` into the `part` field and `snippet` into the `part` field you will next want to enter the Channel ID that you recorded from the previous step into the `channelId` field. Your request is now ready! The final request parameters should look like the following:

   ![YouTube API Playlists Final Request Parameters](./img/YouTubeAPI_PlaylistsRequestParams.png)
6. The next step is executing and sending the request to the API. Scroll to the bottom of the *Try this API* modal and uncheck the Google OAuth 2.0 box. Make sure that API Key is checked, and you're ready to execute the request. Click on the *EXECUTE* button.

   ![YouTube API Execute Request](./img/YouTubeAPI_ExecuteRequest.png)
7. You just sent the request and the response will open in a small window in the bottom right with the HTTP status code and the application/json response. For our example with the `computerphile` YouTube channel (Channel ID: `UC9-y-6csu5WGm29I7JiwpnA`) the request returned `totalResults: 35`. The `computerphile` channel has 35 different playlists. Scroll through the results and look for the playlist you want the ID for. For this example we will use "*Pong, Python & Pygame*". (Look at the highlighted portion in the example screenshot to find the Playlist ID) This is the Playlist ID for the "*Pong, Python & Pygame*" playlist on the `computerphile` YouTube channel.
Under the `snippet` section of the response you can find further details about the return playlist(s) such as `title` and `description`. Use the details in the `snippet` section to confirm the correct playlist. **Make note of the `id` as this is the Playlist ID resource you need.**

   ![YouTube API Playlists Request Response](./img/YouTubeAPI_PlaylistsRequestResponse.png)

**NOTE** A channel may have hundreds of playlists, so it's important to use the snippet section to confirm the details of the playlist to ensure you record the correct Playlist ID.

## **Conclusion**

You now have all the tools you need to acquire the three resources you need to use YouTube with **JosephGuadagno.NET Broadcasting**.

For example these are the three resources we found in this tutorial.

***NOTE***: These are for demonstration/tutorial purposes only and are not valid keys.

| Key | Value |
| --- | --- |
| YouTube API key | `AIzaSyCI8CIzYPIuvJ7-pkw11-8cf9tgXg5rQtk` |
| Channel ID | `UC9-y-6csu5WGm29I7JiwpnA` |
| Playlist ID | `PLzH6n4zXuckoUWpzSEpQNW6I8rXIzyi8w` |
