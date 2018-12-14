using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Taxi.Helpers.Creational
{
    public class RedisConnectionFactory
    {
        private static readonly Lazy<ConnectionMultiplexer> Connection;

        static RedisConnectionFactory()
        {
            try
            {
               // var connectionString = "85.25.11.9:2720,password=d6d255496fbbd00973e7f13dd2a014ce";
                var connectionString = "localhost";

                var options = ConfigurationOptions.Parse(connectionString);

                Connection = new Lazy<ConnectionMultiplexer>(
                    () => ConnectionMultiplexer.Connect(options)
                );
            }
            catch (Exception e)
            {
                var ex = e;
            }            
        }

        public static ConnectionMultiplexer GetConnection() => Connection.Value;

    }

}
