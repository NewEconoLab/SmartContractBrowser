using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Bson.IO;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;

namespace remotebuilderCore
{
    class mongoHelper
    {
        public string mongodbConnStr = string.Empty;
        public string mongodbDatabase = string.Empty;

        public mongoHelper()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection()    //将配置文件的数据加载到内存中
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())   //指定配置文件所在的目录
                .AddJsonFile("mongodbsettings.json", optional: true, reloadOnChange: true)  //指定加载的配置文件
                .Build();    //编译成对象  
            mongodbConnStr= config["mongodbConnStr"];
            mongodbDatabase = config["mongodbDatabase"];
        }

        public void insertJson2MongoOnlyonce(string json,string key)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>("contractWarehouse");

            var query = collection.Find(BsonDocument.Parse("{hash:'"+ key + "'}")).ToList();

            if (query.Count == 0)
            {
                collection.InsertOne(BsonDocument.Parse(json));
            }     

            client = null;
        }
    }
}
