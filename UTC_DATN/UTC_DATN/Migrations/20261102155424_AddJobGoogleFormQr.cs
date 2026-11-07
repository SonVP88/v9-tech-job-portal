using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTC_DATN.Migrations
{
    /// <inheritdoc />
    public partial class AddJobGoogleFormQr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignFormSource",
                table: "Jobs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleFormEditUrl",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleFormId",
                table: "Jobs",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleFormPublicUrl",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeImagePath",
                table: "Jobs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignFormSource",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "GoogleFormEditUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "GoogleFormId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "GoogleFormPublicUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "QrCodeImagePath",
                table: "Jobs");
        }
    }
}
