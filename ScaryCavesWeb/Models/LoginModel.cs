using System.ComponentModel.DataAnnotations;

namespace ScaryCavesWeb.Models;

public class LoginModel
{
    [Required, StringLength(20, MinimumLength = 4), RegularExpression("^[a-zA-Z]*$", ErrorMessage = "Only Letters allowed")]
    public string? PlayerName { get; set; }
    [Required, StringLength(20, MinimumLength = 4)]
    public string? Password { get; set; }
    [Required]
    public string? Token { get; set; }
}
