using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SımpleBankingApp.Web.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        [Required]

        public int AccountId { get; set; }

        public int Amount { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
    }
}
