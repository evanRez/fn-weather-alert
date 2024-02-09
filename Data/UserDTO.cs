using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserDTO 
{
    public Guid UserId;
    public string Email;

    public string Name;

    public int TimeId;

    public int Active;

}