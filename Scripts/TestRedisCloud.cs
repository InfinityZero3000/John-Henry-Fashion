using StackExchange.Redis;
using System;

namespace JohnHenryFashionWeb.Scripts
{
    /// <summary>
    /// Test Redis Cloud Connection
    /// Run: dotnet run --project "John Henry Website.csproj" Scripts/TestRedisCloud.cs
    /// </summary>
    public class TestRedisCloud
    {
        // Commented out to avoid entry point conflict with Program.cs
        // Uncomment and run separately if needed for testing
        /*
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Redis Cloud Connection ===\n");
            
            try
            {
                // Configure Redis Cloud connection
                var config = new ConfigurationOptions
                {
                    EndPoints = { { "redis-19621.c238.us-central1-2.gce.redns.redis-cloud.com", 19621 } },
                    User = "default",
                    Password = "UxJWb9BBbwr6vqfa8GRRkQIFaCwIQGcT",
                    AbortOnConnectFail = false,
                    ConnectTimeout = 10000,
                    SyncTimeout = 5000,
                    AllowAdmin = false,
                    Ssl = true, // Redis Cloud usually requires SSL
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                };
                
                Console.WriteLine("Connecting to Redis Cloud...");
                var muxer = ConnectionMultiplexer.Connect(config);
                
                if (muxer.IsConnected)
                {
                    Console.WriteLine("Connected successfully!\n");
                    
                    var db = muxer.GetDatabase();
                    
                    // Test 1: Set a value
                    Console.WriteLine("Test 1: Setting key 'test_key' = 'Hello from John Henry Fashion'");
                    db.StringSet("test_key", "Hello from John Henry Fashion");
                    
                    // Test 2: Get the value
                    Console.WriteLine("Test 2: Getting key 'test_key'");
                    RedisValue result = db.StringGet("test_key");
                    Console.WriteLine($"Result: {result}\n");
                    
                    // Test 3: Set with expiration
                    Console.WriteLine("Test 3: Setting key 'temp_key' with 60 second expiration");
                    db.StringSet("temp_key", "This will expire in 60 seconds", TimeSpan.FromSeconds(60));
                    
                    // Test 4: Get TTL
                    var ttl = db.KeyTimeToLive("temp_key");
                    Console.WriteLine($"TTL: {ttl?.TotalSeconds} seconds\n");
                    
                    // Test 5: Store JSON object
                    Console.WriteLine("Test 5: Storing JSON object");
                    var testObject = new { Name = "John Henry", Location = "Vietnam", Items = 100 };
                    var json = System.Text.Json.JsonSerializer.Serialize(testObject);
                    db.StringSet("test_json", json);
                    
                    var retrievedJson = db.StringGet("test_json");
                    Console.WriteLine($"Retrieved JSON: {retrievedJson}\n");
                    
                    // Test 6: Get all keys
                    Console.WriteLine("Test 6: Listing all keys");
                    var server = muxer.GetServer(muxer.GetEndPoints()[0]);
                    var keys = server.Keys(database: 0, pattern: "*");
                    
                    Console.WriteLine($"Total keys: {keys.Count()}");
                    foreach (var key in keys.Take(10))
                    {
                        Console.WriteLine($"  - {key}");
                    }
                    
                    // Test 7: Increment counter
                    Console.WriteLine("\nTest 7: Increment counter");
                    db.StringIncrement("visit_count");
                    db.StringIncrement("visit_count");
                    db.StringIncrement("visit_count");
                    var count = db.StringGet("visit_count");
                    Console.WriteLine($"Visit count: {count}");
                    
                    // Test 8: Hash operations
                    Console.WriteLine("\nTest 8: Hash operations");
                    db.HashSet("user:1001", new HashEntry[] {
                        new HashEntry("name", "Nguyen Van A"),
                        new HashEntry("email", "nguyenvana@example.com"),
                        new HashEntry("phone", "0123456789")
                    });
                    
                    var userName = db.HashGet("user:1001", "name");
                    Console.WriteLine($"User name: {userName}");
                    
                    // Cleanup test keys
                    Console.WriteLine("\n=== Cleaning up test keys ===");
                    db.KeyDelete("test_key");
                    db.KeyDelete("temp_key");
                    db.KeyDelete("test_json");
                    db.KeyDelete("visit_count");
                    db.KeyDelete("user:1001");
                    
                    Console.WriteLine("All tests passed!");
                    
                    muxer.Close();
                }
                else
                {
                    Console.WriteLine("Failed to connect to Redis Cloud");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n=== Test completed ===");
        }
        */
    }
}
