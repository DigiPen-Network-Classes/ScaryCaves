using System.ComponentModel.DataAnnotations;

namespace ScaryCavesWeb.Models;

public class RegisterModel
{
    [Required, StringLength(20, MinimumLength = 4), RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Only Letters and Numbers allowed")]
    public string? PlayerName { get; set; }

    [Required, StringLength(20, MinimumLength = 4)]
    public string? Password { get; set; }

    // TODO add captcha!
}
