using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? Name { get; set; }
    public string? Curp { get; set; }
    public string? Rfc { get; set; }


}