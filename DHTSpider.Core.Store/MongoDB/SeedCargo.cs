using MongoDB.Driver;
using System.Linq;
using Tancoder.Torrent.BEncoding;

namespace DHTSpider.Core.Store.MongoDB
{
    public class SeedCargo
    {
        public string ConnectionString { get; set; }
        public MongoClient Client { get; protected set; }
        public IMongoDatabase Database { get; protected set; }
        public IMongoCollection<Seed> Seeds { get; protected set; }

        public SeedCargo(string constr = "mongodb://localhost/")
        {
            Client = new MongoClient(constr);
            Database = Client.GetDatabase("Spider");
            Seeds = Database.GetCollection<Seed>("Seeds");
        }

        public void Add(BEncodedDictionary info, byte[] hash)
        {
            if (ExsistHash(hash))
            {
                IncHot(hash);
            }
            else
            {
                Add(Seed.FromMetadata(info, hash));
            }
        }

        public void IncHot(byte[] hash)
        {
            var filter = Builders<Seed>.Filter.Eq(n => n.Infohash, hash);
            var update = Builders<Seed>.Update.Inc(n => n.HotCount, 1);
            Seeds.FindOneAndUpdate(filter, update);
        }

        public void Add(Seed seed)
        {
            Seeds.InsertOne(seed);
        }

        public bool ExsistHash(byte[] infohash)
        {
            try
            {
                var filter = Builders<Seed>.Filter.Eq(n => n.Infohash, infohash);
                var result = Seeds.Find(filter);
                return result.Any();
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        public Seed GetSeed(byte[] infohash)
        {
            var filter = Builders<Seed>.Filter.Eq(n => n.Infohash, infohash);
            var result = Seeds.Find(filter);
            return result.First();
            //return Seeds.AsQueryable().Where(n => n.Infohash.SequenceEqual(infohash)).Any();
        }
    }
}
