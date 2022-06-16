using CsvHelper.Configuration.Attributes;

namespace GtslNumbanGen.Models
{
    public class Account
    {
        [Index(0)]
        public string SerialNo { get; set; } = string.Empty;

        [Index(1)]
        public string AccountNo { get; set; } = string.Empty;

        [Index(2)]
        public string OldAccountNo { get; set; } = string.Empty;

        [Index(3)]
        public string AccountName { get; set; } = string.Empty;

        public string NubanNo { get; set; } = string.Empty;
    }
}
