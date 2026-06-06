namespace ThreatCastPK.Database.Models
{
    public class Location
    {
        public Guid Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Navigation properties
        public ICollection<AttackEvent> AttackEvents { get; set; } = new List<AttackEvent>();
    }
}