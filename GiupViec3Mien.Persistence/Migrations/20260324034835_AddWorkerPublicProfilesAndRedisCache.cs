using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiupViec3Mien.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerPublicProfilesAndRedisCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DesiredJobTitle",
                table: "WorkerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesiredServiceCategories",
                table: "WorkerProfiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfilePublic",
                table: "WorkerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocations",
                table: "WorkerProfiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeekingDescription",
                table: "WorkerProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesiredJobTitle",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "DesiredServiceCategories",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "IsProfilePublic",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLocations",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "SeekingDescription",
                table: "WorkerProfiles");
        }
    }
}
