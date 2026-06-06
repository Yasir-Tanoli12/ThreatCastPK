using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreatCastPK.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorRiskScoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdatedAt",
                table: "SectorRiskScores",
                newName: "LastCalculatedAt");

            migrationBuilder.RenameColumn(
                name: "EventCount",
                table: "SectorRiskScores",
                newName: "EventCount24h");

            migrationBuilder.AddColumn<string>(
                name: "AffectedCities",
                table: "ThreatAdvisories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "DiscussionPosts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedCities",
                table: "ThreatAdvisories");

            migrationBuilder.RenameColumn(
                name: "LastCalculatedAt",
                table: "SectorRiskScores",
                newName: "LastUpdatedAt");

            migrationBuilder.RenameColumn(
                name: "EventCount24h",
                table: "SectorRiskScores",
                newName: "EventCount");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "DiscussionPosts",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
