using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTC_DATN.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_ChatbotInfrastructure_Clean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FeedbackId",
                table: "ChatMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Intent",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IntentConfidence",
                table: "ChatMessages",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeMs",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "WasEscalated",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApplicationViews",
                columns: table => new
                {
                    ViewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationViews", x => x.ViewId);
                    table.ForeignKey(
                        name: "FK_ApplicationViews_Applications",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId");
                    table.ForeignKey(
                        name: "FK_ApplicationViews_Users",
                        column: x => x.ViewerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ChatAnalytics",
                columns: table => new
                {
                    AnalyticsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Intent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IntentConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    ResponseLength = table.Column<int>(type: "int", nullable: true),
                    UserRating = table.Column<int>(type: "int", nullable: true),
                    WasEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EntityCount = table.Column<int>(type: "int", nullable: true),
                    EntityConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatAnalytics", x => x.AnalyticsId);
                    table.ForeignKey(
                        name: "FK_ChatAnalytics_ChatMessage",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "ChatMessageId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatAnalytics_User",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotFaqs",
                columns: table => new
                {
                    FaqId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "General"),
                    Keywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotFaqs", x => x.FaqId);
                });

            migrationBuilder.CreateTable(
                name: "ChatFeedbacks",
                columns: table => new
                {
                    FeedbackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ChatSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserSentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatFeedbacks", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_ChatFeedback_ChatMessage",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "ChatMessageId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatFeedback_ChatSession",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "ChatSessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatFeedback_User",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CodeChallenge",
                columns: table => new
                {
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProblemStatement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitialCodeText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeChallenge", x => x.ChallengeId);
                });

            migrationBuilder.CreateTable(
                name: "CandidateCodeSubmission",
                columns: table => new
                {
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCodeSubmission", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_CandidateCodeSubmission_CodeChallenge_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "CodeChallenge",
                        principalColumn: "ChallengeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiCodeReviewReport",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    TimeComplexity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpaceComplexity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeSmells = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityVulnerabilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallFeedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Suggestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCodeReviewReport", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_AiCodeReviewReport_CandidateCodeSubmission_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "CandidateCodeSubmission",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Skills_NormalizedName",
                table: "Skills",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_JobTags_NormalizedName",
                table: "JobTags",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiCodeReviewReport_SubmissionId",
                table: "AiCodeReviewReport",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationViews_ApplicationId",
                table: "ApplicationViews",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationViews_ViewerId",
                table: "ApplicationViews",
                column: "ViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCodeSubmission_ChallengeId",
                table: "CandidateCodeSubmission",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnalytics_CreatedAt",
                table: "ChatAnalytics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnalytics_Intent",
                table: "ChatAnalytics",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnalytics_MessageId",
                table: "ChatAnalytics",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnalytics_UserId",
                table: "ChatAnalytics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotFaqs_IsActive_Priority",
                table: "ChatbotFaqs",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatFeedback_ChatSessionId",
                table: "ChatFeedbacks",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatFeedback_MessageId",
                table: "ChatFeedbacks",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatFeedback_UserId",
                table: "ChatFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatFeedbacks_MessageId",
                table: "ChatFeedbacks",
                column: "MessageId",
                unique: true,
                filter: "[MessageId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCodeReviewReport");

            migrationBuilder.DropTable(
                name: "ApplicationViews");

            migrationBuilder.DropTable(
                name: "ChatAnalytics");

            migrationBuilder.DropTable(
                name: "ChatbotFaqs");

            migrationBuilder.DropTable(
                name: "ChatFeedbacks");

            migrationBuilder.DropTable(
                name: "CandidateCodeSubmission");

            migrationBuilder.DropTable(
                name: "CodeChallenge");

            migrationBuilder.DropColumn(
                name: "FeedbackId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Intent",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IntentConfidence",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "WasEscalated",
                table: "ChatMessages");

            migrationBuilder.CreateIndex(
                name: "UQ_Skills_NormalizedName",
                table: "Skills",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_JobTags_NormalizedName",
                table: "JobTags",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");
        }
    }
}
