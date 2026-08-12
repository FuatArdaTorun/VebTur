using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VebTur.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_HotelId",
                table: "RoomTypes");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_HotelId_IsActive_BaseNightlyPrice",
                table: "RoomTypes",
                columns: new[] { "HotelId", "IsActive", "BaseNightlyPrice" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_HotelId_IsActive_BaseNightlyPrice",
                table: "RoomTypes");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_HotelId",
                table: "RoomTypes",
                column: "HotelId");
        }
    }
}
