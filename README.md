# AsyncBatcher

A .NET 4.5 library for batching multiple async calls into less batched calls, which is useful for batching together remote calls, e.g. database reads.

Batching database calls helps in three ways:

1. reduced network calls to the database
2. reduced database load due to more efficent use of the database
3. reduced number of connections to the database, helping to avoid database connection pool exhaustion.

For example, say you have a data access layer for _orders_ that gets an order via an integer id:
```csharp
...
using BusterWood.Mapper; // easy reading and mapping from the database

public class OrderDataAccess 
{
  private readonly _connectionString;
  
  public OrderDataAccess(string connectionString)
  {
    _connectionString = connectionString;
  }
  
  public async Task<Order> FindByIdAsync(int orderId) 
  {
    using (var cnn = new SqlConnection(_connectionString))
    {
      await cnn.OpenAsync();
      return await cnn.QueryAsync<Order>("select * from [order] where order_id=@orderId", new {orderId})
                      .SingleOrDefaultAsync();
    }
  }
}
```

The `FindByIdAsync` is fine for one-at-a-time order loading, but what if you had hunderds or thousands of asynchonous tasks all reading at the same time?  This is where async batching comes in, grouping to together calls for efficency based on batching time, e.g. group toegther calls for 100ms (the default).  

Using `AsyncFuncBatcher` or `AsyncFuncManyBatcher` you can reduce thousands of database calls into tens of calls.

Here is an example of batching the above OrderDataAccess call:

```csharp
...
using BusterWood.Mapper; // easy reading and mapping from the database

public class OrderDataAccess 
{
  private readonly _connectionString;
  private readonly AsyncFuncBatcher<int, Order> _orderBatcher;
  
  public OrderDataAccess(string connectionString)
  {
    _connectionString = connectionString;
    _orderBatcher = new AsyncFuncBatcher<int, Order>(ids => FindByManyIdsAsync(ids));
  }
  
  public async Task<Order> FindByIdAsync(int orderId) 
  {
    return await _orderBatcher.QueryAsync(orderId);
  }

  private async Task<Dictionary<int, Order>> FindByManyIdsAsync(IReadOnlyCollection<int> orderIds) 
  {
    // use a SQL Server Table-Valued-Parameter
    var idsTable = orderIds.ToTableType(new [] { new SqlMetaData("ID", SqlType.Int) }, "IdType");
    using (var cnn = new SqlConnection(_connectionString))
    {
      await cnn.OpenAsync();
      return await cnn.QueryAsync<Order>("select o.* from [order] o join @idsTable ids on ids.id = o.id", new {idsTable})
                      .ToDictionaryAsync(ord => ord.Id);
    }
  }
}
```

There is also a class for batching together asynchonous actions `AsyncActionBatcher<T>`, which can be used to group together actions into batches.
This may be useful for sending one batched message rather than a lots of smaller messages.

Gotchas
-------

Please be aware of that these batchers will _not_ work with ambient transactions, i.e. `System.Transactions`.