using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityArchitect.Example.Migrations
{
    /// <inheritdoc />
    public partial class Accesed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accesses",
                table: "__EndpointMap",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accesses",
                table: "__EndpointMap");
        }
    }
}
