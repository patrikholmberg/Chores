using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chores.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChoreAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "AttemptWindow",
                table: "Families",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(7, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "BasePoints",
                table: "Families",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateTable(
                name: "ChoreAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PointMultiplier = table.Column<int>(type: "integer", nullable: false),
                    StartDay = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoreAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoreAttempts_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChoreAttempts_Members_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChoreAttemptSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReminderDelay = table.Column<TimeSpan>(type: "interval", nullable: true),
                    RepeatInterval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoreAttemptSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoreAttemptSteps_ChoreAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ChoreAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreAttempts_ChildId",
                table: "ChoreAttempts",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoreAttempts_FamilyId",
                table: "ChoreAttempts",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoreAttempts_FamilyId_ChildId_StartDay",
                table: "ChoreAttempts",
                columns: new[] { "FamilyId", "ChildId", "StartDay" });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreAttemptSteps_AttemptId",
                table: "ChoreAttemptSteps",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoreAttemptSteps_FamilyId",
                table: "ChoreAttemptSteps",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChoreAttemptSteps");

            migrationBuilder.DropTable(
                name: "ChoreAttempts");

            migrationBuilder.DropColumn(
                name: "AttemptWindow",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "BasePoints",
                table: "Families");
        }
    }
}
