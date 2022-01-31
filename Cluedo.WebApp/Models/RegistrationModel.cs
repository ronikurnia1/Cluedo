using System.ComponentModel.DataAnnotations;

namespace Cluedo.WebApp.Models;

public class RegistrationModel
{
    [Required(ErrorMessage = "Player name is required.")]
    [StringLength(20, ErrorMessage = "Player name is too long.")]
    [Display(Description = "Player name")]
    public string Name { get; set; } = string.Empty;
}
