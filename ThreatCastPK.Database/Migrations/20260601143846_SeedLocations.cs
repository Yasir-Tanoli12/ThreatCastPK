using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ThreatCastPK.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "CityName", "Latitude", "Longitude", "Province" },
                values: new object[,]
                {
                    { new Guid("2c72a6e8-1a6e-4ec8-8c3f-1a8b5c7a7007"), "Peshawar", 34.015099999999997, 71.580500000000001, "Khyber Pakhtunkhwa" },
                    { new Guid("3a3dce2d-0a91-4f9c-9c6d-4c0b0b3f2c01"), "Karachi", 24.860700000000001, 67.001099999999994, "Sindh" },
                    { new Guid("51c8a5d6-65d2-44d6-a1e9-5f3d2a8b8008"), "Quetta", 30.1798, 66.974999999999994, "Balochistan" },
                    { new Guid("6d1c9f83-4c4c-4f8a-8ad9-1c2d3e4f1011"), "Sialkot", 32.494500000000002, 74.522900000000007, "Punjab" },
                    { new Guid("7f78f3a4-58a6-4cf1-8f2b-9e0f2c3a6006"), "Multan", 30.157499999999999, 71.524900000000002, "Punjab" },
                    { new Guid("9a1d4570-4b62-4a6d-86bb-5c9d5c5a9c02"), "Lahore", 31.520399999999999, 74.358699999999999, "Punjab" },
                    { new Guid("ab93c7b8-1f2c-4a66-9e2a-7c8d9e0f1212"), "Abbottabad", 34.146299999999997, 73.211699999999993, "Khyber Pakhtunkhwa" },
                    { new Guid("b0c39e1f-ef6f-41b1-8d7b-3c5aa1f0e003"), "Islamabad", 33.684399999999997, 73.047899999999998, "Islamabad Capital Territory" },
                    { new Guid("c9a7e2b1-6e3a-4e69-8b2f-6d6c7a7f0010"), "Gujranwala", 32.1877, 74.194500000000005, "Punjab" },
                    { new Guid("d2a2c9a4-5a0a-4b82-a6e5-2f1b8c1c5004"), "Rawalpindi", 33.565100000000001, 73.016900000000007, "Punjab" },
                    { new Guid("e8b6c5f4-1c9a-4c12-9d6a-2b8f25d6e005"), "Faisalabad", 31.450399999999998, 73.135000000000005, "Punjab" },
                    { new Guid("f4a86b77-0d9f-4c76-9f2f-9b7e8b6c9009"), "Hyderabad", 25.396000000000001, 68.357799999999997, "Sindh" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("2c72a6e8-1a6e-4ec8-8c3f-1a8b5c7a7007"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("3a3dce2d-0a91-4f9c-9c6d-4c0b0b3f2c01"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("51c8a5d6-65d2-44d6-a1e9-5f3d2a8b8008"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("6d1c9f83-4c4c-4f8a-8ad9-1c2d3e4f1011"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("7f78f3a4-58a6-4cf1-8f2b-9e0f2c3a6006"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("9a1d4570-4b62-4a6d-86bb-5c9d5c5a9c02"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("ab93c7b8-1f2c-4a66-9e2a-7c8d9e0f1212"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("b0c39e1f-ef6f-41b1-8d7b-3c5aa1f0e003"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("c9a7e2b1-6e3a-4e69-8b2f-6d6c7a7f0010"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("d2a2c9a4-5a0a-4b82-a6e5-2f1b8c1c5004"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("e8b6c5f4-1c9a-4c12-9d6a-2b8f25d6e005"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("f4a86b77-0d9f-4c76-9f2f-9b7e8b6c9009"));
        }
    }
}
