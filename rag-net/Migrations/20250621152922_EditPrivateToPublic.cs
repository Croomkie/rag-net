using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_net.Migrations
{
    /// <inheritdoc />
    public partial class EditPrivateToPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EmbeddingChunk",
                table: "EmbeddingChunk");

            migrationBuilder.RenameTable(
                name: "EmbeddingChunk",
                newName: "EmbeddingChunks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmbeddingChunks",
                table: "EmbeddingChunks",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EmbeddingChunks",
                table: "EmbeddingChunks");

            migrationBuilder.RenameTable(
                name: "EmbeddingChunks",
                newName: "EmbeddingChunk");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmbeddingChunk",
                table: "EmbeddingChunk",
                column: "Id");
        }
    }
}
