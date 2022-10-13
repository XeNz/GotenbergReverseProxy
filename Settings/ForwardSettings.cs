namespace GotenbergReverseProxy.Settings;

public class ForwardSettings
{
    public ForwardSettings()
    {
    }

    public ForwardSettings(string gotenbergInstanceUrl)
    {
        GotenbergInstanceUrl = gotenbergInstanceUrl;
    }

    public string GotenbergInstanceUrl { get; set; }
}