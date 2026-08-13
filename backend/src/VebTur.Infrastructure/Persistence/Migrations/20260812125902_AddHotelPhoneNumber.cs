using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VebTur.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Hotels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Hotels");
        }
    }
}
