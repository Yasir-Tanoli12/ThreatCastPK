using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreatCastPK.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixDiscussionPostFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_Users_UserId",
                table: "DiscussionPosts");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "DiscussionPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_UserId1",
                table: "DiscussionPosts",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts",
                column: "ParentPostId",
                principalTable: "DiscussionPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_Users_UserId",
                table: "DiscussionPosts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_Users_UserId1",
                table: "DiscussionPosts",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_Users_UserId",
                table: "DiscussionPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_Users_UserId1",
                table: "DiscussionPosts");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionPosts_UserId1",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "DiscussionPosts");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_DiscussionPosts_ParentPostId",
                table: "DiscussionPosts",
                column: "ParentPostId",
                principalTable: "DiscussionPosts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_Users_UserId",
                table: "DiscussionPosts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
