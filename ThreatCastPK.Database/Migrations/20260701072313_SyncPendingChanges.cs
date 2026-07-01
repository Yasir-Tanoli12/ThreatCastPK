using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreatCastPK.Database.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Downvotes",
                table: "DiscussionPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "DiscussionPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "DiscussionPosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "DiscussionPosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Upvotes",
                table: "DiscussionPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "DiscussionPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Downvotes",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "Upvotes",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "DiscussionPosts");
        }
    }
}
