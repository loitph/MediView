using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediView.Studies.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStudyPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "priority",
                schema: "studies",
                table: "studies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "priority",
                schema: "studies",
                table: "studies",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }
    }
}
