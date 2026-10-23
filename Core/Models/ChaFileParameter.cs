using MessagePack;

namespace KoikatsuCardReader.Core.Models
{
    /// <summary>
    /// Minimal model for the "Parameter" block (ChaFileParameter).
    /// Only the basic fields are declared. Unknown keys are skipped by MessagePack.
    /// Field names match KK and KKS (KKS only adds exType / interest).
    /// </summary>
    [MessagePackObject]
    public class ChaFileParameter
    {
        public const string BlockName = "Parameter";

        [Key("sex")]
        public byte Sex { get; set; }

        [Key("lastname")]
        public string LastName { get; set; }

        [Key("firstname")]
        public string FirstName { get; set; }

        [Key("nickname")]
        public string NickName { get; set; }
    }
}
