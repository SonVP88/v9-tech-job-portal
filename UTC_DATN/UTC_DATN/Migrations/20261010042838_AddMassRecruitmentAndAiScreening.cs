using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTC_DATN.Migrations
{
    /// <inheritdoc />
    public partial class AddMassRecruitmentAndAiScreening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedStartDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FilledHeadcount",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "RecruitmentPriority",
                table: "Jobs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "Shift",
                table: "Jobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TargetHeadcount",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "WorkZone",
                table: "Jobs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkforceCategory",
                table: "Jobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AiAnalysisSummary",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "AiMatchingScore",
                table: "Applications",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModelVersion",
                table: "Applications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AiProcessedAt",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProcessingError",
                table: "Applications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AiProcessingStatus",
                table: "Applications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedSkills",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RecruitmentPriority",
                table: "Jobs",
                column: "RecruitmentPriority");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_WorkforceCategory",
                table: "Jobs",
                column: "WorkforceCategory");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_WorkZone_Status",
                table: "Jobs",
                columns: new[] { "WorkZone", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AiMatchingScore",
                table: "Applications",
                column: "AiMatchingScore");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AiProcessingStatus",
                table: "Applications",
                column: "AiProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobId_AiScore",
                table: "Applications",
                columns: new[] { "JobId", "AiMatchingScore" });

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Manager_Self",
                table: "Users",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Manager_Self",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_RecruitmentPriority",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_WorkforceCategory",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_WorkZone_Status",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Applications_AiMatchingScore",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_AiProcessingStatus",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_JobId_AiScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpectedStartDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FilledHeadcount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RecruitmentPriority",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TargetHeadcount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkZone",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkforceCategory",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AiAnalysisSummary",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiMatchingScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiModelVersion",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiProcessedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiProcessingError",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiProcessingStatus",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ExtractedSkills",
                table: "Applications");
        }
    }
}
