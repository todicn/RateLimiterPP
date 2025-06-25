using RateLimiter.Implementations;

Console.WriteLine("RateLimiter Demo");
Console.WriteLine("================");

try
{
    var tokenBucket = new TokenBucket(capacity: 5, refillRate: 1.0);
    
    Console.WriteLine("Testing Token Bucket...");
    Console.WriteLine("Capacity: 5 tokens, Refill rate: 1 token/second");
    Console.WriteLine();

    // TODO: Once implemented, this will demonstrate the rate limiter
    var allowed = await tokenBucket.TryConsumeAsync();
    Console.WriteLine($"Request allowed: {allowed}");
}
catch (NotImplementedException)
{
    Console.WriteLine("Token bucket not implemented yet!");
    Console.WriteLine("Implement the TryConsumeAsync method to see this demo work.");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey(); 