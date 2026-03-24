using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiupViec3Mien.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefineRecruitmentFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WorkDate",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkEndTime",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkStartTime",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableStartDate",
                table: "JobApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JobApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkEndTime",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkStartTime",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AvailableStartDate",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JobApplications");
        }
    }
}
