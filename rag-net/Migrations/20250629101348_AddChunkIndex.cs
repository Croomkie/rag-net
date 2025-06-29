using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_net.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "EmbeddingChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                table: "EmbeddingChunks");
        }
    }
}
