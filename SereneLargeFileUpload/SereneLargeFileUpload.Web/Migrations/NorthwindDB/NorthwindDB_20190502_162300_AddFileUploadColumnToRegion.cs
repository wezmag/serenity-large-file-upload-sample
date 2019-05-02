using FluentMigrator;

namespace SereneLargeFileUpload.Migrations.NorthwindDB
{
    [Migration(20190502162300)]
    public class NorthwindDB_20190502_162300_AddFileUploadColumnToRegion : AutoReversingMigration
    {
        public override void Up()
        {
            this.Alter.Table("Region")
                .AddColumn("FileUpload1").AsString(int.MaxValue).Nullable()
                .AddColumn("FileUpload2").AsString(int.MaxValue).Nullable();
        }
    }
}
