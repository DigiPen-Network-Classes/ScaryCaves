using System.ComponentModel.DataAnnotations;

namespace ScaryCavesWeb.Models;

public class LoginModel
{
    [Required]
    public string PlayerName { get; set; }
    [Required]
    public string Password { get; set; }
}
