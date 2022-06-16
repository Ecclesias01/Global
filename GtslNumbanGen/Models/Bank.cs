using System.ComponentModel.DataAnnotations;

namespace GtslNumbanGen.Models
{
    public class Bank
    {
        [StringLength(6, ErrorMessage = "Bank code required is 6 digit")]
        public string BankCode { get; set; } = "990046";

        public string SerialNo { get; set; } = string.Empty;
        public string NubanNo { get; set; } = string.Empty;
    }
}
