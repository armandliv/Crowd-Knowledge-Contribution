using Microsoft.AspNetCore.Identity;

namespace ProiectASP.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Article>? Articles { get; set; }
    }
}
