using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Codewisp.Yammer.Models;

namespace Codewisp.Yammer
{
    public class Migrations : DataMigrationImpl
    {

        public int Create()
        {
            // Creating table YammerWidgetPartRecord
            SchemaBuilder.CreateTable("YammerWidgetPartRecord", table => table
                .ContentPartRecord()
                .Column("ItemsToDisplay", DbType.Int32)
                .Column("CacheMinutes", DbType.Int32)
                .Column("ClientId", DbType.String)
                .Column("ClientSecret", DbType.String)
                .Column("SelectedNetwork", DbType.String)
                .Column("SelectedNetworkPermalink", DbType.String)
                .Column("SelectedNetworkAccessToken", DbType.String)
            );

            ContentDefinitionManager.AlterPartDefinition(typeof(YammerWidgetPart).Name, builder => builder.Attachable());

            ContentDefinitionManager.AlterTypeDefinition("YammerWidget", builder => builder
                .WithPart(typeof(YammerWidgetPart).Name)
                .WithPart("CommonPart")
                .WithPart("WidgetPart")
                .WithSetting("Stereotype", "Widget"));

            return 1;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.AlterTable("YammerWidgetPartRecord", table => table.AddColumn("AuthAccessToken", DbType.String));
            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.AlterTable("YammerWidgetPartRecord", table => table.DropColumn("SelectedNetwork"));
            SchemaBuilder.AlterTable("YammerWidgetPartRecord", table => table.AddColumn("SelectedNetworkName", DbType.String));
            return 3;
        }

        public int UpdateFrom3()
        {
            SchemaBuilder.AlterTable("YammerWidgetPartRecord", table => table.DropColumn("SelectedNetworkName"));
            SchemaBuilder.AlterTable("YammerWidgetPartRecord", table => table.DropColumn("SelectedNetworkPermalink"));
            return 4;
        }
    }
}