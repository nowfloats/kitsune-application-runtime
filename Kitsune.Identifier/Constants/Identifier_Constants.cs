using KitsuneLayoutManager.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Constants
{
    public class Identifier_Constants
    {
        #region MONGO DB SERVER DETAILS

        //Kitsune
        public static string KitsuneMongoServerUrl { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneCoreMongoServer.ServerUrl; } }
        public static string KitsuneDatabaseName { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneCoreMongoServer.DatabaseName; } }

        //Kitsune Schema
        public static string KitsuneSchemaMongoServerUrl { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneSchemaMongoServer.ServerUrl; } }
        public static string KitsuneSchemaDatabaseName { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneSchemaMongoServer.DatabaseName; } }


        #endregion

        #region MONGO DB COLLECTIONS

        public static string KitsuneWebsiteCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneWebsiteCollectionName; } }
        public static string KitsuneWebsiteUserCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneWebsiteUserCollectionName; } }
        public static string KitsuneDNSCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneDNSCollectionName; } }
        public static string KitsuneProjectProductionCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneProjectProductionCollectionName; } }
        public static string KitsuneResourcesProductionCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneResourcesProductionCollectionName; } }
        public static string KitsuneProjectCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneProjectCollectionName; } }
        public static string KitsuneResourcesCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneResourcesCollectionName; } }

        #endregion
    }
}
