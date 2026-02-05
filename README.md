# Bluesky Autolive (via Streamer.bot)

## Description
This extension will automatically set your live status on Bluesky and link to your Twitch stream. 

## Import Code
1. From the Streamer.bot start page, click the "→| Import" button.
2. Copy the latest version of the code below from the [Releases](https://github.com/plzdebugmycode/bsky-autolive/releases) page into the "Import String" box. It should automatically populate with the following info:
 - Name: Bluesky Autolive
 - Author: plzdebugmycode
 - Export Version: 1.0.0
 - Streamer.bot Version: 1.0.4
 - Actions: Stream Start
3. Click the "Import" button.
4. Streamer.bot will warn you that "What you are importing contains custom C# code." Press the "Yes" button.
 - (A friendly reminder to never run random code from the internet that you do not trust)

## Configuration
### Global Variables
This extension requires three persistent Global Variables in order to run.
| Global Variable Name | Usage |
|----|----|
| autoLive_bskyAppPassword | A Blusky App Password for your account (DO NOT USE YOUR BSKY PASSWORD). To generate an app password, check [here](https://bsky.app/settings/app-passwords). |
| autoLive_bskyIdentifier | Your Bluesky handle without the @ (i.e. mine is "plzdebugmycode.com") |
| autoLive_streamDefaultLength | The time that you want your stream to be live on Bluesky measured in minutes. Values can be 1-240. |
  
If you Navigate to the "Stream Start" action, right-click on the "OBS Studio - Streaming Started" trigger, and click "Test Trigger", these variables will be automatically populated in your Persistent Global Variable list but you will still need to populate them.

## Future Work
- Add a refreshing timer so that you remain live, even after the four-hour mark.
- Remove your live status when stream ends.
