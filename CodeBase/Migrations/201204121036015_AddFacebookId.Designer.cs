// <auto-generated />
namespace CodeBase.Migrations
{
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Migrations.Infrastructure;
    
    public sealed partial class AddFacebookId : IMigrationMetadata
    {
        string IMigrationMetadata.Id
        {
            get { return "201204121036015_AddFacebookId"; }
        }
        
        string IMigrationMetadata.Source
        {
            get { return null; }
        }
        
        string IMigrationMetadata.Target
        {
            get { return "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"; }
        }
    }
}
