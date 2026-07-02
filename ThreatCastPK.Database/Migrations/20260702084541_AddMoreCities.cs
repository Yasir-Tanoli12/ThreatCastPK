using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreatCastPK.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "CityName", "Province", "Latitude", "Longitude" },
                values: new object[,]
                {
            // Punjab
            { Guid.NewGuid(), "Bahawalpur",       "Punjab",               29.3956, 71.6836 },
            { Guid.NewGuid(), "Sargodha",          "Punjab",               32.0836, 72.6711 },
            { Guid.NewGuid(), "Sheikhupura",       "Punjab",               31.7167, 73.9850 },
            { Guid.NewGuid(), "Rahim Yar Khan",    "Punjab",               28.4202, 70.2952 },
            { Guid.NewGuid(), "Jhang",             "Punjab",               31.2681, 72.3181 },
            { Guid.NewGuid(), "Dera Ghazi Khan",   "Punjab",               30.0588, 70.6350 },
            { Guid.NewGuid(), "Gujrat",            "Punjab",               32.5736, 74.0790 },
            { Guid.NewGuid(), "Sahiwal",           "Punjab",               30.6706, 73.1064 },
            { Guid.NewGuid(), "Wah Cantonment",    "Punjab",               33.7667, 72.7167 },
            { Guid.NewGuid(), "Kasur",             "Punjab",               31.1167, 74.4500 },
            { Guid.NewGuid(), "Okara",             "Punjab",               30.8138, 73.4534 },
            { Guid.NewGuid(), "Chiniot",           "Punjab",               31.7167, 72.9833 },
            { Guid.NewGuid(), "Kamoke",            "Punjab",               31.9742, 74.2228 },
            { Guid.NewGuid(), "Hafizabad",         "Punjab",               32.0714, 73.6883 },
            { Guid.NewGuid(), "Khanewal",          "Punjab",               30.3014, 71.9322 },
            { Guid.NewGuid(), "Bahawalnagar",      "Punjab",               29.9922, 73.2546 },
            { Guid.NewGuid(), "Pakpattan",         "Punjab",               30.3436, 73.3887 },
            { Guid.NewGuid(), "Mandi Bahauddin",   "Punjab",               32.5862, 73.4917 },
            { Guid.NewGuid(), "Jhelum",            "Punjab",               32.9361, 73.7258 },
            { Guid.NewGuid(), "Khushab",           "Punjab",               32.2986, 72.3522 },
            { Guid.NewGuid(), "Attock",            "Punjab",               33.7664, 72.3602 },
            { Guid.NewGuid(), "Chakwal",           "Punjab",               32.9328, 72.8557 },
            { Guid.NewGuid(), "Toba Tek Singh",    "Punjab",               30.9709, 72.4826 },
            { Guid.NewGuid(), "Vehari",            "Punjab",               30.0454, 72.3513 },
            { Guid.NewGuid(), "Muzaffargarh",      "Punjab",               30.0736, 71.1930 },
            { Guid.NewGuid(), "Lodhran",           "Punjab",               29.5343, 71.6322 },
            { Guid.NewGuid(), "Layyah",            "Punjab",               30.9614, 70.9378 },
            { Guid.NewGuid(), "Rajanpur",          "Punjab",               29.1044, 70.3296 },

            // Sindh
            { Guid.NewGuid(), "Sukkur",            "Sindh",                27.7052, 68.8574 },
            { Guid.NewGuid(), "Larkana",           "Sindh",                27.5570, 68.2247 },
            { Guid.NewGuid(), "Mirpur Khas",       "Sindh",                25.5270, 69.0138 },
            { Guid.NewGuid(), "Nawabshah",         "Sindh",                26.2442, 68.4100 },
            { Guid.NewGuid(), "Jacobabad",         "Sindh",                28.2769, 68.4514 },
            { Guid.NewGuid(), "Shikarpur",         "Sindh",                27.9558, 68.6378 },
            { Guid.NewGuid(), "Khairpur",          "Sindh",                27.5295, 68.7592 },
            { Guid.NewGuid(), "Dadu",              "Sindh",                26.7319, 67.7750 },
            { Guid.NewGuid(), "Thatta",            "Sindh",                24.7461, 67.9239 },
            { Guid.NewGuid(), "Badin",             "Sindh",                24.6557, 68.8397 },
            { Guid.NewGuid(), "Tharparkar",        "Sindh",                24.7136, 70.2461 },
            { Guid.NewGuid(), "Sanghar",           "Sindh",                26.0461, 68.9483 },
            { Guid.NewGuid(), "Matiari",           "Sindh",                25.5942, 68.4611 },

            // Khyber Pakhtunkhwa
            { Guid.NewGuid(), "Mardan",            "Khyber Pakhtunkhwa",   34.1985, 72.0404 },
            { Guid.NewGuid(), "Mingora",           "Khyber Pakhtunkhwa",   34.7717, 72.3600 },
            { Guid.NewGuid(), "Dera Ismail Khan",  "Khyber Pakhtunkhwa",   31.8314, 70.9019 },
            { Guid.NewGuid(), "Kohat",             "Khyber Pakhtunkhwa",   33.5869, 71.4414 },
            { Guid.NewGuid(), "Bannu",             "Khyber Pakhtunkhwa",   32.9889, 70.6042 },
            { Guid.NewGuid(), "Swabi",             "Khyber Pakhtunkhwa",   34.1197, 72.4697 },
            { Guid.NewGuid(), "Nowshera",          "Khyber Pakhtunkhwa",   34.0153, 71.9747 },
            { Guid.NewGuid(), "Charsadda",         "Khyber Pakhtunkhwa",   34.1483, 71.7306 },
            { Guid.NewGuid(), "Mansehra",          "Khyber Pakhtunkhwa",   34.3333, 73.2000 },
            { Guid.NewGuid(), "Haripur",           "Khyber Pakhtunkhwa",   33.9942, 72.9353 },
            { Guid.NewGuid(), "Karak",             "Khyber Pakhtunkhwa",   33.1167, 71.0833 },
            { Guid.NewGuid(), "Tank",              "Khyber Pakhtunkhwa",   32.2189, 70.3775 },
            { Guid.NewGuid(), "Chitral",           "Khyber Pakhtunkhwa",   35.8511, 71.7864 },

            // Balochistan
            { Guid.NewGuid(), "Turbat",            "Balochistan",          26.0025, 63.0422 },
            { Guid.NewGuid(), "Khuzdar",           "Balochistan",          27.8000, 66.6167 },
            { Guid.NewGuid(), "Hub",               "Balochistan",          25.0550, 66.9908 },
            { Guid.NewGuid(), "Chaman",            "Balochistan",          30.9200, 66.4597 },
            { Guid.NewGuid(), "Gwadar",            "Balochistan",          25.1264, 62.3225 },
            { Guid.NewGuid(), "Dera Bugti",        "Balochistan",          29.0333, 69.1667 },
            { Guid.NewGuid(), "Sibi",              "Balochistan",          29.5433, 67.8775 },
            { Guid.NewGuid(), "Zhob",              "Balochistan",          31.3417, 69.4486 },
            { Guid.NewGuid(), "Nushki",            "Balochistan",          29.5522, 66.0206 },
            { Guid.NewGuid(), "Panjgur",           "Balochistan",          26.9683, 64.0992 },

            // AJK & GB
            { Guid.NewGuid(), "Muzaffarabad",      "Azad Kashmir",         34.3700, 73.4700 },
            { Guid.NewGuid(), "Mirpur",            "Azad Kashmir",         33.1467, 73.7508 },
            { Guid.NewGuid(), "Rawalakot",         "Azad Kashmir",         33.8578, 73.7622 },
            { Guid.NewGuid(), "Gilgit",            "Gilgit-Baltistan",     35.9208, 74.3083 },
            { Guid.NewGuid(), "Skardu",            "Gilgit-Baltistan",     35.2972, 75.6333 },
            { Guid.NewGuid(), "Hunza",             "Gilgit-Baltistan",     36.3167, 74.6500 },

            // ICT
            { Guid.NewGuid(), "Wah",               "Punjab",               33.7667, 72.7167 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        DELETE FROM ""Locations""
        WHERE ""CityName"" IN (
            'Bahawalpur','Sargodha','Sheikhupura','Rahim Yar Khan','Jhang',
            'Dera Ghazi Khan','Gujrat','Sahiwal','Wah Cantonment','Kasur',
            'Okara','Chiniot','Kamoke','Hafizabad','Khanewal','Bahawalnagar',
            'Pakpattan','Mandi Bahauddin','Jhelum','Khushab','Attock','Chakwal',
            'Toba Tek Singh','Vehari','Muzaffargarh','Lodhran','Layyah','Rajanpur',
            'Sukkur','Larkana','Mirpur Khas','Nawabshah','Jacobabad','Shikarpur',
            'Khairpur','Dadu','Thatta','Badin','Tharparkar','Sanghar','Matiari',
            'Mardan','Mingora','Dera Ismail Khan','Kohat','Bannu','Swabi',
            'Nowshera','Charsadda','Mansehra','Haripur','Karak','Tank','Chitral',
            'Turbat','Khuzdar','Hub','Chaman','Gwadar','Dera Bugti','Sibi',
            'Zhob','Nushki','Panjgur',
            'Muzaffarabad','Mirpur','Rawalakot','Gilgit','Skardu','Hunza','Wah'
        )");
        }
    }
}
