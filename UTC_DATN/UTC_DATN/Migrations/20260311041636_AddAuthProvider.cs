using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTC_DATN.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyDescription",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyIndustry",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyLogoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyWebsite",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedById",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedByName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Skills",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedById",
                table: "Jobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByName",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedById",
                table: "InterviewEvaluations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "CandidateDocuments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "CandidateDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CoverLetters",
                columns: table => new
                {
                    CoverLetterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverLetters", x => x.CoverLetterId);
                    table.ForeignKey(
                        name: "FK_CoverLetters_Candidates",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RelatedId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedJobs",
                columns: table => new
                {
                    SavedJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedJobs", x => x.SavedJobId);
                    table.ForeignKey(
                        name: "FK_SavedJobs_Candidates",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedJobs_Jobs",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvaluations_SubmittedById",
                table: "InterviewEvaluations",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoverLetters_CandidateId",
                table: "CoverLetters",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_JobId",
                table: "SavedJobs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "UQ_SavedJobs_Candidate_Job",
                table: "SavedJobs",
                columns: new[] { "CandidateId", "JobId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewEvaluations_Users_SubmittedById",
                table: "InterviewEvaluations",
                column: "SubmittedById",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewEvaluations_Users_SubmittedById",
                table: "InterviewEvaluations");

            migrationBuilder.DropTable(
                name: "CoverLetters");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SavedJobs");

            migrationBuilder.DropIndex(
                name: "IX_InterviewEvaluations_SubmittedById",
                table: "InterviewEvaluations");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyDescription",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyIndustry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyLogoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyWebsite",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedByName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ClosedByName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SubmittedById",
                table: "InterviewEvaluations");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "CandidateDocuments");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "CandidateDocuments");
        }
    }
}
