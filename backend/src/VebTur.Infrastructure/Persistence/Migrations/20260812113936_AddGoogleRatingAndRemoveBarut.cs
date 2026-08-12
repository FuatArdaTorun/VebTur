using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VebTur.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleRatingAndRemoveBarut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GoogleRating",
                table: "Hotels",
                type: "numeric(2,1)",
                precision: 2,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GoogleRatingCapturedAtUtc",
                table: "Hotels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoogleRatingCount",
                table: "Hotels",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleRating",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "GoogleRatingCapturedAtUtc",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "GoogleRatingCount",
                table: "Hotels");
        }
    }
}
