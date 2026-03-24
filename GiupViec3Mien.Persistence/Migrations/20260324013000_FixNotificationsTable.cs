using GiupViec3Mien.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiupViec3Mien.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324013000_FixNotificationsTable")]
    public class FixNotificationsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'Notifications'
                    ) THEN
                        CREATE TABLE "Notifications" (
                            "Id" uuid NOT NULL,
                            "RecipientId" uuid NOT NULL,
                            "Type" text NOT NULL,
                            "Title" text NOT NULL,
                            "Message" text NOT NULL,
                            "Link" text NULL,
                            "IsRead" boolean NOT NULL,
                            "CreatedAt" timestamp with time zone NOT NULL,
                            CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
                            CONSTRAINT "FK_Notifications_Users_RecipientId"
                                FOREIGN KEY ("RecipientId")
                                REFERENCES "Users" ("Id")
                                ON DELETE CASCADE
                        );
                    END IF;
                END
                $$;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Notifications_RecipientId_IsRead_CreatedAt"
                ON "Notifications" ("RecipientId", "IsRead", "CreatedAt");
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "Notifications";
                """
            );
        }
    }
}
