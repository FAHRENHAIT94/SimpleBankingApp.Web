using System.ComponentModel.DataAnnotations;

namespace SımpleBankingApp.Web.Models
{
    public class Account
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int Balance { get; set; }
        public int UserId { get; set; }

    }
}
