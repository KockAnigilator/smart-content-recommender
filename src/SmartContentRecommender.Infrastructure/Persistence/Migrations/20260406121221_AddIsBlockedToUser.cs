using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartContentRecommender.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBlockedToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "users");
        }
    }
}
