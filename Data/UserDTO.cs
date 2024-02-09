namespace data_weatheralert;

public class UserDTO 
{
    public Guid UserId {get; set;}
    public string Email {get; set;}

    public string Name {get; set;}

    public int TimeId {get; set;}

    public bool Active {get; set;}

    public string Latitude {get; set;}
    public string Longitude {get; set;}
}
