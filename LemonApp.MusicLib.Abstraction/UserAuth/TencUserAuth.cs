namespace LemonApp.MusicLib.Abstraction.UserAuth;
public class TencUserAuth
{
    public string Id { get; set; } = "0";
    public string Cookie { get; set; }=string.Empty;
    public string G_tk { get; set; } = string.Empty;

    public bool IsValid => Id != "0";
}