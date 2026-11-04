using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chores.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuartzJobStore : Migration
    {
        // Keep in sync with the pinned Quartz package version in Directory.Packages.props.
        private const string ScriptResource = "Chores.Infrastructure.Persistence.Migrations.Sql.quartz_4.7.1.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema("quartz");
            migrationBuilder.Sql(ReadScript());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS quartz CASCADE;");
        }

        private static string ReadScript()
        {
            using var stream = typeof(AddQuartzJobStore).Assembly.GetManifestResourceStream(ScriptResource)
                ?? throw new InvalidOperationException($"Embedded resource '{ScriptResource}' not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
