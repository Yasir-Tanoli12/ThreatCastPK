using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreatCastPK.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddForumFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostedAt",
                table: "DiscussionPosts",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DiscussionPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentPostId",
                table: "DiscussionPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "DiscussionPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts",
                column: "ParentPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts",
                column: "ParentPostId",
                principalTable: "DiscussionPosts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "ParentPostId",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "DiscussionPosts");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "DiscussionPosts",
                newName: "PostedAt");
        }
    }
}
