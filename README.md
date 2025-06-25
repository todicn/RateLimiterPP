# RateLimiter - Interview Challenge

A minimal C# project structure for implementing rate limiting algorithms.

## Getting Started

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project RateLimiter.ConsoleApp
```

## Your Task

Implement different rate limiting algorithms:

1. **Token Bucket** - Allow bursts up to a capacity, refill at steady rate
2. **Fixed Window** - Simple counter that resets at fixed intervals  
3. **Sliding Window** - More accurate tracking over time windows

## Discussion Points

- Algorithm trade-offs (memory vs accuracy)
- Thread safety considerations
- Distributed systems challenges
- Testing strategies
- Performance characteristics

Start with the `IRateLimiter` interface and implement your chosen algorithm! 