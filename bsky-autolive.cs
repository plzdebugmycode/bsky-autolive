using System;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CPHInline
{

    private const string _sessionEndpoint = "https://bsky.social/xrpc/com.atproto.server.createSession";
    private static readonly HttpClient _httpClient = new HttpClient{Timeout = TimeSpan.FromSeconds(15)};

    private const string _defaultBskyIdString = "BSKY_USERNAME_HERE";
    private const string _defaultBskyAppPassString = "BSKY_APP_PASS_HERE";
    private const int _defaultStreamLenString = 1;

    public void Init() {
        _httpClient.DefaultRequestHeaders.Clear();

        List<GlobalVariableValue> globalVarList = CPH.GetGlobalVarValues(true);
        List<string> globalVarStringList = new List<string>();

        foreach(GlobalVariableValue globalVar in globalVarList){
            globalVarStringList.Add(globalVar.VariableName);
        }

        string[] requiredGlobals = ["autoLive_bskyIdentifier","autoLive_bskyAppPassword","autoLive_streamDefaultLength"];

        if(!globalVarStringList.Contains("autoLive_bskyIdentifier")){
            CPH.SetGlobalVar("autoLive_bskyIdentifier", _defaultBskyIdString, true);
        }

        if(!globalVarStringList.Contains("autoLive_bskyAppPassword")){
            CPH.SetGlobalVar("autoLive_bskyAppPassword", _defaultBskyAppPassString, true);
        }

        if(!globalVarStringList.Contains("autoLive_streamDefaultLength")){
            CPH.SetGlobalVar("autoLive_streamDefaultLength", _defaultStreamLenString, true);
        }
    }

    public void Dispose() {
        _httpClient.Dispose();
    }

    public bool Execute()
    {   
        // Get the required global variables
		var autoLive_bskyIdentifier = CPH.GetGlobalVar<string>("autoLive_bskyIdentifier", true);
        var autoLive_bskyAppPassword = CPH.GetGlobalVar<string>("autoLive_bskyAppPassword", true);
        var autoLive_streamDefaultLength = CPH.GetGlobalVar<int>("autoLive_streamDefaultLength", true);

        // We want to be able to print all the missing globals, so keep track of the error here
        bool errExit = false;

        // Error out if autoLive_bskyIdentifier isn't set
        if(autoLive_bskyIdentifier == _defaultBskyIdString){
            CPH.LogError("BskyAutoLive :: Set the global variable \"autoLive_bskyIdentifier\" before running this code.");
            errExit |= true;
        }

        // Error out if autoLive_bskyAppPassword isn't set
        if(autoLive_bskyAppPassword == _defaultBskyAppPassString){
            CPH.LogError("BskyAutoLive :: Set the global variable \"autoLive_bskyAppPassword\" before running this code.");
            errExit |= true;
        }

        // Error out if autoLive_streamDefaultLength isn't set
        if(autoLive_streamDefaultLength == _defaultStreamLenString){
            CPH.LogError("BskyAutoLive :: Set the global variable \"autoLive_streamDefaultLength\" before running this code.");
            errExit |= true;
        }

        if(errExit){
            CPH.LogError("BskyAutoLive :: Exiting this action because globals weren't setup properly.");
            return false;
        }

        // com.atproto.server.createSession POST body defition
        // https://docs.bsky.app/docs/api/com-atproto-server-create-session
        // identifier - string
        // password - string
        var loginData = new {
            identifier = autoLive_bskyIdentifier,
            password = autoLive_bskyAppPassword
        };

        // Convert the POST body into a string to send to HTTP endpoint
        string loginJsonData = JsonConvert.SerializeObject(loginData);
        // Apply appropriate encoding for HTTP POST request body content
        StringContent postContent = new StringContent(loginJsonData, System.Text.Encoding.UTF8, "application/json");

        // Create an Uri (mostly so Streamer.bot knows to include the System.dll) 
        Uri getSessionUri = new Uri(_sessionEndpoint);

        // Send the HTTP POST requst. It's a async menthod, so get the Awaiter and await the result
        HttpResponseMessage authResponse;
        try{
            authResponse = _httpClient.PostAsync(getSessionUri, postContent).GetAwaiter().GetResult();
        }
        catch(InvalidOperationException e){
            CPH.LogError("BskyAutoLive :: There is an issue with the URI formation. It is targeting [" + _sessionEndpoint + "]. Contact @plzdebugmycode.");
            return false;
        }
        catch(HttpRequestException e){
            CPH.LogError("BskyAutoLive :: There was an issue contacting [" + _sessionEndpoint + "]. Check connection settings and Bsky status.");
            return false;
        }
        catch(Exception e){
            CPH.LogError("BskyAutoLive :: An unhandled exception has ocurred. Send this log to PlzDebugMyCode.");
            CPH.LogDebug("BskyAutoLive :: " + e.ToString());
            return false;
        }

        // Get the response data that contains the auth information. It's a async menthod, so get the Awaiter and await the result
        string sessionContentString = authResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        // Deserialize the auth token into the ATSession object for easier manipulation
        ATSession session;
        try{
            session = JsonConvert.DeserializeObject<ATSession>(sessionContentString);
        }
        catch(JsonSerializationException){
            CPH.LogError("BskyAutoLive :: The returned data content was not as expected. Send this log to PlzDebugMyCode.");
            CPH.LogDebug("BskyAutoLive :: " + sessionContentString);
            return false;
        }
        
        // Declare the twitch username ahead of time just incase the user isn't logged in
        string twitchUsername;

        // TwitchGetBroadcaster will throw a generic exception if a user isn't logged in as a broadcaster, so be ready to catch that
        try{
            TwitchUserInfo userInfo = CPH.TwitchGetBroadcaster();
            twitchUsername = userInfo.UserName;
        }
        catch(Exception e){
            CPH.LogError("BskyAutoLive :: You must be logged into Streamer.bot as a Twitch broadcaster to use this action.");
            return false;
        }

        // com.atproto.repo.putRecord POST body defition
        // https://docs.bsky.app/docs/api/com-atproto-repo-put-record
        var liveOnTwitchData = new
        {
            collection = "app.bsky.actor.status",
            record = new
            {
                type = "app.bsky.actor.status",
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                durationMinutes = autoLive_streamDefaultLength,
                embed = new Dictionary<string, object>
                {
                    { "$type", "app.bsky.embed.external"},
                    { "external", new
                    {
                        type = "app.bsky.embed.external#external",
                        description = "Check me out live on Twitch!",
                        title = twitchUsername + " - Twitch",
                        uri = "https://www.twitch.tv/" + twitchUsername
                    } }
                },
                status = "app.bsky.actor.status#live"
            },
            repo = session.did,
            rkey = "self",
            
        };

        // Convert the POST body into a string to send to HTTP endpoint
        string liveOnTwitchJsonData = JsonConvert.SerializeObject(liveOnTwitchData);
        // Apply appropriate encoding for HTTP POST request body content
        postContent = new StringContent(liveOnTwitchJsonData, System.Text.Encoding.UTF8, "application/json");

        // Apply the authorization token to the HTTP headers to perform authenticated actions
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.accessJwt);

        // Create an Uri (mostly so Streamer.bot knows to include the System.dll) 
        Uri goLiveUri = new Uri(session.didDoc.service[0].serviceEndpoint + "/xrpc/com.atproto.repo.putRecord");

        // Send the HTTP POST requst. It's a async menthod, so get the Awaiter and await the result
        HttpResponseMessage goLiveResponse;
        try{
            goLiveResponse = _httpClient.PostAsync(goLiveUri, postContent).GetAwaiter().GetResult();
        }
        catch(InvalidOperationException e){
            CPH.LogError("BskyAutoLive :: There is an issue with the URI formation. It is targeting [" + goLiveUri.ToString() + "]. Contact @plzdebugmycode.");
            return false;
        }
        catch(HttpRequestException e){
            CPH.LogError("BskyAutoLive :: There was an issue contacting [" + goLiveUri.ToString() + "]. Check connection settings and Bsky status.");
            return false;
        }
        catch(Exception e){
            CPH.LogError("BskyAutoLive :: An unhandled exception has ocurred. Send this log to PlzDebugMyCode.");
            CPH.LogDebug("BskyAutoLive :: " + e.ToString());
            return false;
        }

        CPH.LogDebug(goLiveResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        return true;
    }

    
}

// Autogenerated Objects for JSON Deserialization
public class ATSession
{
    public string did { get; set; }
    public Diddoc didDoc { get; set; }
    public string handle { get; set; }
    public string email { get; set; }
    public bool emailConfirmed { get; set; }
    public bool emailAuthFactor { get; set; }
    public string accessJwt { get; set; }
    public string refreshJwt { get; set; }
    public bool active { get; set; }
}

public class Diddoc
{
    public string[] context { get; set; }
    public string id { get; set; }
    public string[] alsoKnownAs { get; set; }
    public Verificationmethod[] verificationMethod { get; set; }
    public Service[] service { get; set; }
}

public class Verificationmethod
{
    public string id { get; set; }
    public string type { get; set; }
    public string controller { get; set; }
    public string publicKeyMultibase { get; set; }
}

public class Service
{
    public string id { get; set; }
    public string type { get; set; }
    public string serviceEndpoint { get; set; }
}

public class Rootobject
{
    public string repo { get; set; }
    public string collection { get; set; }
    public string rkey { get; set; }
    public Record record { get; set; }
    public object swapRecord { get; set; }
}

public class Record
{
    public string type { get; set; }
    public DateTime createdAt { get; set; }
    public string status { get; set; }
    public int durationMinutes { get; set; }
    public Embed embed { get; set; }
}

public class Embed
{
    public string type { get; set; }
    public External external { get; set; }
}

public class External
{
    public string type { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string uri { get; set; }
    public Thumb thumb { get; set; }
}

public class Thumb
{
    public string type { get; set; }
    public Ref _ref { get; set; }
    public string mimeType { get; set; }
    public int size { get; set; }
}

public class Ref
{
    public string link { get; set; }
}