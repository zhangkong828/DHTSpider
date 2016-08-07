using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Tancoder.Torrent.BEncoding;

namespace DHTSpider.Core.Store.MongoDB
{
    public class Seed
    {
        [BsonId]
        [BsonRepresentation(BsonType.Binary)]
        public byte[] Infohash { get; set; }

        [BsonDefaultValue(1)]
        public int HotCount { get; set; }

        public BsonDocument Info { get; set; }

        public static Seed FromMetadata(BEncodedDictionary metadata, byte[] hash)
        {
            Seed seed = new Seed();

            BsonDocument root = ConvertBEncodedDictionary(metadata);
            seed.Info = root;
            seed.Infohash = hash;

            return seed;
        }

        private static BsonDocument ConvertBEncodedDictionary(BEncodedDictionary dictionary)
        {
            var root = new BsonDocument();
            foreach (var item in dictionary)
            {
                var key = item.Key.Text.Replace('.', ' ');
                var value = item.Value;
                if (key == "pieces")
                    continue;
                if (value is BEncodedString)
                {
                    string str = (value as BEncodedString).Text;
                    root[key] = str;
                }
                else if (value is BEncodedNumber)
                {
                    long num = (value as BEncodedNumber).Number;
                    root[key] = num;
                }
                else if (value is BEncodedDictionary)
                {
                    BsonDocument dict = ConvertBEncodedDictionary(value as BEncodedDictionary);
                    root[key] = dict;
                }
                else if (value is BEncodedList)
                {
                    BsonArray array = ConvertCEncodedList(value as BEncodedList);
                    root[key] = array;
                }
            }

            return root;
        }

        private static BsonArray ConvertCEncodedList(BEncodedList list)
        {
            var root = new BsonArray();
            foreach (var item in list)
            {
                if (item is BEncodedString)
                {
                    string str = (item as BEncodedString).Text;
                    root.Add(str);
                }
                else if (item is BEncodedNumber)
                {
                    long num = (item as BEncodedNumber).Number;
                    root.Add(num);
                }
                else if (item is BEncodedDictionary)
                {
                    BsonDocument dict = ConvertBEncodedDictionary(item as BEncodedDictionary);
                    root.Add(dict);
                }
                else if (item is BEncodedList)
                {
                    BsonArray array = ConvertCEncodedList(item as BEncodedList);
                    root.Add(array);
                }
            }

            return root;
        }
    }
}
