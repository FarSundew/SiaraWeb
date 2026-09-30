using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalSubjectGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinalSubjectGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CutoffDateId = table.Column<int>(type: "int", nullable: false),
                    TotalStudents = table.Column<int>(type: "int", nullable: false),
                    ApprovedRegularStudents = table.Column<int>(type: "int", nullable: false),
                    ApprovedMakeupStudents = table.Column<int>(type: "int", nullable: false),
                    FinalApprovedStudents = table.Column<int>(type: "int", nullable: false),
                    FinalFailedStudents = table.Column<int>(type: "int", nullable: false),
                    FinalDroppedStudents = table.Column<int>(type: "int", nullable: false),
                    FinalApprovalPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FinalFailurePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FinalDropoutPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalSubjectGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalSubjectGrades_CutoffDates_CutoffDateId",
                        column: x => x.CutoffDateId,
                        principalTable: "CutoffDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalSubjectGrades_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinalSubjectGrades_CutoffDateId",
                table: "FinalSubjectGrades",
                column: "CutoffDateId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalSubjectGrades_SubjectId",
                table: "FinalSubjectGrades",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinalSubjectGrades");
        }
    }
}
